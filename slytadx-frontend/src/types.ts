export interface ChatMessagePayload 
{
    Type: string;
    MessageID: string;
    Username: string;
    UserColor: string;
    Message: string;
    IsMod: boolean;
    IsVip: boolean;
    IsBroadcaster: boolean;
}

export interface DbFollower {
    UserID: string;
    Username: string;
    FollowDate: string;
    IsModerator: number;
    Message_Count: number;
}

export interface FollowerListPayload {
    Type: 'FollowerList';
    Data: DbFollower[];
}