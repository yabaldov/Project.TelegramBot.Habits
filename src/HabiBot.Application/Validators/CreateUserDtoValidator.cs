using FluentValidation;
using HabiBot.Application.DTOs;

namespace HabiBot.Application.Validators;

/// <summary>
/// Валидатор для создания пользователя
/// </summary>
public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Имя пользователя обязательно")
            .MaximumLength(200).WithMessage("Имя пользователя не должно превышать 200 символов");

        RuleFor(x => x.TelegramChatId)
            .GreaterThan(0).WithMessage("Telegram ID должен быть положительным числом");

        RuleFor(x => x.TimeZone)
            .MaximumLength(100).WithMessage("Название часового пояса не должно превышать 100 символов")
            .When(x => !string.IsNullOrEmpty(x.TimeZone));
    }
}
