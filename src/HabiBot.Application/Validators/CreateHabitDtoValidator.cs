using FluentValidation;
using HabiBot.Application.DTOs;

namespace HabiBot.Application.Validators;

/// <summary>
/// Валидатор для создания привычки
/// </summary>
public class CreateHabitDtoValidator : AbstractValidator<CreateHabitDto>
{
    public CreateHabitDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название привычки обязательно")
            .MaximumLength(500).WithMessage("Название привычки не должно превышать 500 символов");

        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("ID пользователя должен быть положительным числом");

        RuleFor(x => x.ReminderTime)
            .Matches(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$")
            .WithMessage("Время напоминания должно быть в формате HH:mm (например, 08:00)")
            .When(x => !string.IsNullOrEmpty(x.ReminderTime));

        RuleFor(x => x.Frequency)
            .IsInEnum().WithMessage("Некорректное значение частоты выполнения");
    }
}
