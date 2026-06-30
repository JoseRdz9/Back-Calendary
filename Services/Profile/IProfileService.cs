public interface IProfileService
{
    Task<ProfileResponseDto> GetProfileAsync(Guid userId);
    Task<ProfileResponseDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
}
