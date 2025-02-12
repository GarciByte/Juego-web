export enum RoomAction {
    Bot = 'Bot',
    Random = 'Random',
    Friend = 'Friend',
    CancelRandom = 'CancelRandom',
    CancelRoom = 'CancelRoom',
}

export interface GameRoomAction {
    Action: RoomAction;
    FriendId: number | null;
}