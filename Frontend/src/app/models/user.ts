import { Image } from "./image"

export interface User {
    userId : number,
    nickname : string,
    email : string,
    avatar : Image,
    role: string
}
