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

        RuleFor(x => x.TelegramUserId)
            .GreaterThan(0).WithMessage("Telegram User ID должен быть положительным числом");

        RuleFor(x => x.TelegramChatId)
            .GreaterThan(0).WithMessage("Telegram Chat ID должен быть положительным числом");

        RuleFor(x => x.TelegramFirstName)
            .MaximumLength(100).WithMessage("Имя в Telegram не должно превышать 100 символов")
            .When(x => !string.IsNullOrEmpty(x.TelegramFirstName));

        RuleFor(x => x.TelegramLastName)
            .MaximumLength(100).WithMessage("Фамилия в Telegram не должна превышать 100 символов")
            .When(x => !string.IsNullOrEmpty(x.TelegramLastName));

        RuleFor(x => x.TelegramUserName)
            .MaximumLength(100).WithMessage("Username в Telegram не должен превышать 100 символов")
            .When(x => !string.IsNullOrEmpty(x.TelegramUserName));

        RuleFor(x => x.TimeZone)
            .MaximumLength(100).WithMessage("Название часового пояса не должно превышать 100 символов")
            .When(x => !string.IsNullOrEmpty(x.TimeZone));
    }
}
