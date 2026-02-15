using FluentValidation;
using HabiBot.Application.DTOs;

namespace HabiBot.Application.Validators;

/// <summary>
/// Валидатор для обновления привычки
/// </summary>
public class UpdateHabitDtoValidator : AbstractValidator<UpdateHabitDto>
{
    public UpdateHabitDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID привычки должен быть положительным числом");

        RuleFor(x => x.Name)
            .MaximumLength(500).WithMessage("Название привычки не должно превышать 500 символов")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.ReminderTime)
            .Matches(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$")
            .WithMessage("Время напоминания должно быть в формате HH:mm (например, 08:00)")
            .When(x => !string.IsNullOrEmpty(x.ReminderTime));

        RuleFor(x => x.Frequency)
            .IsInEnum().WithMessage("Некорректное значение частоты выполнения")
            .When(x => x.Frequency.HasValue);
    }
}
