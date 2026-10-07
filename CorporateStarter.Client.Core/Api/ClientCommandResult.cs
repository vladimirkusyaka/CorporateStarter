using System.Text.Json;
namespace CorporateStarter.Client.Core.Api;

internal sealed class ClientCommandResult
{
    public bool Succeeded { get; set; }
    public JsonElement Value { get; set; }
    public void EnsureSuccess()
    {
        if (!Succeeded) throw new InvalidDataException("The server did not confirm the command.");
    }
    public Guid CreatedId()
    {
        EnsureSuccess();
        if (Value.ValueKind != JsonValueKind.String || !Value.TryGetGuid(out var id) || id == Guid.Empty)
            throw new InvalidDataException("The server returned an invalid created ID.");
        return id;
    }
}
