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