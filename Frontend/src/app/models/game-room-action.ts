export enum RoomAction {
    Bot = 'Bot',
    Random = 'Random',
    Friend = 'Friend',
    CancelRandom = 'CancelRandom',
    CancelRoom = 'CancelRoom',
    StartGame = 'StartGame'
}

export interface GameRoomAction {
    Action: RoomAction;
    FriendId: number | null;
}