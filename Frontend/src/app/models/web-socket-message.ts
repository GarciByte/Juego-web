export enum MsgType {
    Connection = 'Connection',
    FriendListUpdate = 'FriendListUpdate',
    FriendStatusUpdate = 'FriendStatusUpdate',
    FriendRequestUpdate = 'FriendRequestUpdate',
    GameInvitation = 'GameInvitation',
    StatsUpdate = 'StatsUpdate'
}

export interface WebSocketMessage {
    Type: MsgType;
    Id: number;
    Content: any;
}