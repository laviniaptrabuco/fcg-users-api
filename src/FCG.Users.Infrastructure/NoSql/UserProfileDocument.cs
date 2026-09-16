using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FCG.Users.Infrastructure.NoSql;

[BsonIgnoreExtraElements]
public class UserProfileDocument
{
    // Sem [BsonIgnoreIfDefault] o driver grava "_id: null" explicitamente
    // quando Id nao e setado (em vez de omitir o campo e deixar o Mongo
    // gerar um ObjectId novo) - o indice unico _id_ rejeita o segundo
    // documento com o mesmo _id null (DuplicateKey, code 11000).
    [BsonId]
    [BsonIgnoreIfDefault]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }

    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [BsonElement("bio")]
    public string? Bio { get; set; }

    [BsonElement("avatarUrl")]
    public string? AvatarUrl { get; set; }

    [BsonElement("favoriteGenres")]
    public List<string> FavoriteGenres { get; set; } = new();

    [BsonElement("preferences")]
    public Dictionary<string, string> Preferences { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
