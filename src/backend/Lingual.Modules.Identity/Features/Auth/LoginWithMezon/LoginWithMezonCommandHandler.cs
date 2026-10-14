using Lingual.Modules.Identity.Contracts.DTOs;
using Lingual.Modules.Identity.Domain;
using Lingual.Modules.Identity.Infrastructure.Data;
using Lingual.Modules.Identity.Infrastructure.Services;
using Lingual.SharedKernel;
using Lingual.SharedKernel.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.Identity.Features.Auth.LoginWithMezon;

public class LoginWithMezonCommandHandler(
    IdentityDbContext dbContext,
    IMezonAuthService mezonAuthService,
    IJwtTokenService jwtTokenService,
    ILogger<LoginWithMezonCommandHandler> logger)
    : ICommandHandler<LoginWithMezonCommand, ApiResponse<AuthResultDto>>
{
    public async Task<ApiResponse<AuthResultDto>> Handle(
        LoginWithMezonCommand command, 
        CancellationToken cancellationToken)
    {
        var userInfo = await mezonAuthService.ValidateAndGetUserInfoAsync(command.Request, cancellationToken);
        if (userInfo == null)
        {
            logger.LogWarning("Authentication failed: invalid Mezon token or userinfo could not be verified.");
            return ApiResponse<AuthResultDto>.Fail("Xác thực token Mezon thất bại. Vui lòng kiểm tra lại token.");
        }

        var isNewUser = false;
        var user = await dbContext.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.MezonUserId == userInfo.MezonUserId, cancellationToken);

        if (user == null)
        {
            isNewUser = true;
            user = new User
            {
                Id = Guid.NewGuid(),
                MezonUserId = userInfo.MezonUserId,
                DisplayName = userInfo.DisplayName,
                AvatarUrl = userInfo.AvatarUrl,
                PreferredLocale = "vi-VN",
                Timezone = "Asia/Ho_Chi_Minh",
                Status = "active",
                Role = "learner",
                LastSeenAt = DateTime.UtcNow
            };

            var profile = new LearnerProfile
            {
                UserId = user.Id,
                PlacementStatus = "pending"
            };

            user.Profile = profile;

            await dbContext.Users.AddAsync(user, cancellationToken);
            await dbContext.LearnerProfiles.AddAsync(profile, cancellationToken);

            logger.LogInformation("Provisioned new user {UserId} with Mezon ID {MezonUserId}", user.Id, user.MezonUserId);
        }
        else
        {
            // Update latest display name & avatar if changed in Mezon
            if (!string.IsNullOrWhiteSpace(userInfo.DisplayName))
            {
                user.DisplayName = userInfo.DisplayName;
            }

            if (!string.IsNullOrWhiteSpace(userInfo.AvatarUrl))
            {
                user.AvatarUrl = userInfo.AvatarUrl;
            }

            user.LastSeenAt = DateTime.UtcNow;

            // Ensure learner profile exists
            if (user.Profile == null)
            {
                var profile = new LearnerProfile
                {
                    UserId = user.Id,
                    PlacementStatus = "pending"
                };
                user.Profile = profile;
                await dbContext.LearnerProfiles.AddAsync(profile, cancellationToken);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var (token, expiresIn) = jwtTokenService.GenerateToken(user);

        var userDto = new UserDto
        {
            Id = user.Id,
            MezonUserId = user.MezonUserId,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            PreferredLocale = user.PreferredLocale,
            Timezone = user.Timezone,
            Status = user.Status,
            Role = user.Role,
            LastSeenAt = user.LastSeenAt,
            Profile = user.Profile != null ? new LearnerProfileDto
            {
                UserId = user.Profile.UserId,
                LearningGoal = user.Profile.LearningGoal,
                DailyCommitmentMinutes = user.Profile.DailyCommitmentMinutes,
                ProficiencyLevel = user.Profile.ProficiencyLevel,
                PlacementStatus = user.Profile.PlacementStatus,
                OnboardingCompletedAt = user.Profile.OnboardingCompletedAt
            } : null
        };

        var response = new AuthResultDto
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
            User = userDto,
            IsNewUser = isNewUser,
            OnboardingCompleted = user.Profile?.OnboardingCompletedAt != null
        };

        return ApiResponse<AuthResultDto>.Ok(response, isNewUser ? "Đăng ký tài khoản Mezon thành công." : "Đăng nhập Mezon thành công.");
    }
}
