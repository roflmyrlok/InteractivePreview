using Riok.Mapperly.Abstractions;
using UserService.Application.DTOs;
using UserService.Domain.Entities;

namespace UserService.Application.Mapping;

[Mapper]
public partial class UserMapper
{
	public partial UserDto UserToUserDto(User user);
	public partial IEnumerable<UserDto> UsersToUserDtos(IEnumerable<User> users);
	public partial User CreateUserDtoToUser(CreateUserDto dto);

	[MapperIgnoreSource(nameof(UpdateUserDto.Id))]
	[MapperIgnoreTarget(nameof(User.PasswordHash))]
	[MapperIgnoreTarget(nameof(User.CreatedAt))]
	[MapperIgnoreTarget(nameof(User.UpdatedAt))]
	[MapperIgnoreTarget(nameof(User.Username))]
	[MapperIgnoreTarget(nameof(User.LastLoginDate))]
	[MapperIgnoreTarget(nameof(User.Id))]
	public partial void UpdateUserFromDto(UpdateUserDto dto, User user);
}
