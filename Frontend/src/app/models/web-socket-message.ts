export enum MsgType {
    Connection = 'Connection',
    Disconnect = 'Disconnect',
    FriendListUpdate = 'FriendListUpdate',
    FriendStatusUpdate = 'FriendStatusUpdate',
    FriendRequestUpdate = 'FriendRequestUpdate',
    GameRoom = 'GameRoom',
    StartGame = 'StartGame',
    GameInvitation = 'GameInvitation',
    StatsUpdate = 'StatsUpdate'
}

export interface WebSocketMessage {
    Type: MsgType;
    Id: number;
    Content: any;
}