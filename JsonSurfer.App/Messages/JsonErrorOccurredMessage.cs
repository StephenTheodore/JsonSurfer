using CommunityToolkit.Mvvm.Messaging.Messages;

namespace JsonSurfer.App.Messages;

public class JsonErrorOccurredMessage : ValueChangedMessage<JsonErrorDetails>
{
    public JsonErrorOccurredMessage(JsonErrorDetails value) : base(value)
    {
    }
}

public record JsonErrorDetails(string Message, int Line, int Column);