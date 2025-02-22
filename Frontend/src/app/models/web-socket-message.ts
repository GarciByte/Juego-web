export enum MsgType {
    Connection = 'Connection',
    FriendListUpdate = 'FriendListUpdate',
    FriendStatusUpdate = 'FriendStatusUpdate',
    FriendRequestUpdate = 'FriendRequestUpdate',
    GameRoom = 'GameRoom',
    StartGame = 'StartGame',
    GameInvitation = 'GameInvitation',
    CancelGameInvitation = 'CancelGameInvitation',
    ErrorGameInvitation = 'ErrorGameInvitation',
    StatsUpdate = 'StatsUpdate',
    GameStart = 'GameStart',
    GameUpdate = 'GameUpdate',
    GameOver = 'GameOver',
    Chat = 'Chat',
    RematchRequest = 'RematchRequest',
    CancelRematchRequest = 'CancelRematchRequest'
}

export interface WebSocketMessage {
    Type: MsgType;
    Id: number;
    Content: any;
}