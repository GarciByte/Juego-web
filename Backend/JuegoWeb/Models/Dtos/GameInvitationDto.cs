using System.Text.Json.Serialization;

namespace JuegoWeb.Models.Dtos;

public class GameInvitationDto
{
    public int FromUserId { get; set; }

    public int ToUserId { get; set; }

}