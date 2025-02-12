export enum GameRoomType {
    Bot = 'Bot',
    Random = 'Random',
    Friend = 'Friend'
}

export interface GameRoom {
    RoomId: number;
    HostUserId: number;
    GuestUserId: number;
    RoomType: GameRoomType;
}