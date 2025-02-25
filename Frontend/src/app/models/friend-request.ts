export interface FriendRequest {
    id: number;
    senderId: number;
    senderNickname: string;
    receiverId: number;
    receiverNickname: string;
    isAccepted: boolean;
}