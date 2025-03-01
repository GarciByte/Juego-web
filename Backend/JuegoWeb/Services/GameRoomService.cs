using JuegoWeb.Models.Dtos;

namespace JuegoWeb.Services;

public class GameRoomService
{
    // Lista de salas activas
    private List<GameRoomDto> _activeRooms = new List<GameRoomDto>();

    // Lista de jugadores en espera para jugar contra un oponente aleatorio
    private List<int> _playersWaitingForRandomMatch = new List<int>();

    // Contador para asignar un identificador único a cada sala
    private long _nextRoomId = 1;

    // Semáforo para controlar el acceso a los recursos compartidos
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);


    // Obtener el ID de la sala
    private async Task<long> GetNextRoomId()
    {
        long id;
        await _semaphore.WaitAsync();
        try
        {
            id = _nextRoomId;
            _nextRoomId++;
        }
        finally
        {
            _semaphore.Release();
        }

        return id;
    }

    // Añadir una sala a la lista de salas activas
    private async Task AddRoomAsync(GameRoomDto room)
    {
        await _semaphore.WaitAsync();
        try
        {
            _activeRooms.Add(room);
        }
        finally
        {
            _semaphore.Release();
        }

        //Console.WriteLine($"Salas activas: {_activeRooms.Count}.");
    }

    // Crear una sala en la que se juega contra un bot
    public async Task<GameRoomDto> CreateRoomAgainstBotAsync(int hostUserId)
    {
        var room = new GameRoomDto
        {
            RoomId = await GetNextRoomId(),
            HostUserId = hostUserId,
            GuestUserId = null,
            RoomType = GameRoomType.Bot
        };

        await AddRoomAsync(room);
        return room;
    }

    // Crear una sala buscando un oponente aleatorio
    public async Task<GameRoomDto> SearchRandomOpponentAsync(int userId, CancellationToken token)
    {
        GameRoomDto foundRoom = null;

        // Se busca a un jugador que ya estuviera en la lista de espera
        int waitingOpponent = _playersWaitingForRandomMatch.FirstOrDefault(playerId => playerId != userId);

        if (waitingOpponent != 0)
        {
            await _semaphore.WaitAsync();
            try
            {
                _playersWaitingForRandomMatch.Remove(waitingOpponent);
            }
            finally
            {
                _semaphore.Release();
            }

            // Crear la sala con el oponente encontrado
            foundRoom = new GameRoomDto
            {
                RoomId = await GetNextRoomId(),
                HostUserId = waitingOpponent,
                GuestUserId = userId,
                RoomType = GameRoomType.Random
            };

            await AddRoomAsync(foundRoom);
        }
        else
        {
            // Agrega al usuario a la lista de espera
            if (!_playersWaitingForRandomMatch.Contains(userId))
            {
                await _semaphore.WaitAsync();
                try
                {
                    _playersWaitingForRandomMatch.Add(userId);
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        if (foundRoom != null)
        {
            return foundRoom;
        }

        // El usuario espera hasta que se le empareje o cancele la búsqueda
        try
        {
            while (!token.IsCancellationRequested)
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Revisa si ya se ha creado una sala para el usuario
                    foundRoom = _activeRooms.FirstOrDefault(r =>
                    {
                        bool isRandom = r.RoomType == GameRoomType.Random;
                        bool isUser = r.HostUserId == userId || r.GuestUserId == userId;
                        return isRandom && isUser;
                    });

                    if (foundRoom != null)
                    {
                        return foundRoom;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }

                // Espera antes de volver a comprobar
                await Task.Delay(500, token);
            }
        }
        catch (OperationCanceledException)
        {
            // Si el usuario cancela la búsqueda, se le elimina de la lista de espera
            await _semaphore.WaitAsync();
            try
            {
                _playersWaitingForRandomMatch.RemoveAll(playerId => playerId == userId);
            }
            finally
            {
                _semaphore.Release();
            }
            return null;
        }
        return null;
    }

    // Crear una sala en la que se invita a un amigo
    public async Task<GameRoomDto> CreateRoomByInvitationAsync(int hostUserId, int guestUserId)
    {
        var room = new GameRoomDto
        {
            RoomId = await GetNextRoomId(),
            HostUserId = hostUserId,
            GuestUserId = guestUserId,
            RoomType = GameRoomType.Friend
        };

        await AddRoomAsync(room);
        return room;
    }

    // Gestiona la desconexión de un usuario en una sala
    public async Task<GameRoomDto> HandleUserDisconnectionAsync(int userId)
    {
        GameRoomDto room = null;
        await _semaphore.WaitAsync();
        try
        {
            // Se busca si se encuentra en una sala
            room = _activeRooms.FirstOrDefault(r => r.HostUserId == userId || r.GuestUserId == userId);

            if (room != null)
            {
                switch (room.RoomType)
                {
                    case GameRoomType.Bot:
                        if (room.HostUserId == userId)
                        {
                            _activeRooms.Remove(room);
                        }
                        break;

                    case GameRoomType.Random:
                    case GameRoomType.Friend:
                        if (room.HostUserId == userId)
                        {
                            if (room.GuestUserId != null)
                            {
                                // El invitado se convierte en host
                                room.HostUserId = room.GuestUserId.Value;
                                room.GuestUserId = null;
                            }
                            else
                            {
                                _activeRooms.Remove(room);
                            }
                        }
                        else if (room.GuestUserId == userId)
                        {
                            room.GuestUserId = null;
                        }
                        break;

                    default:
                        break;
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }

        //Console.WriteLine($"Salas activas: {_activeRooms.Count}.");

        // Notificar la actualización de la sala si sigue activa
        if (room != null && room.RoomType != GameRoomType.Bot && _activeRooms.Contains(room))
        {
            return room;
        }

        return null;
    }

    // Obtiene la sala a la que pertenece un usuario
    public async Task<GameRoomDto> GetRoomByUserIdsAsync(int userId)
    {
        GameRoomDto room = null;
        await _semaphore.WaitAsync();
        try
        {
            room = _activeRooms.FirstOrDefault(r => r.HostUserId == userId || r.GuestUserId == userId);
        }
        finally
        {
            _semaphore.Release();
        }
        return room;
    }
}