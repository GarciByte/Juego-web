import { Image } from "./image"

export enum UserStatus {
    Online = 'Online',
    Offline = 'Offline',
    Playing = 'Playing'
}

export interface User {
    userId: number,
    nickname: string,
    email: string,
    avatar: Image,
    role: string,
    status: UserStatus;
}