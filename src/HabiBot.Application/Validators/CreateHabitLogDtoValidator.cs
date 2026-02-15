using FluentValidation;
using HabiBot.Application.DTOs;

namespace HabiBot.Application.Validators;

/// <summary>
/// Валидатор для создания записи выполнения привычки
/// </summary>
public class CreateHabitLogDtoValidator : AbstractValidator<CreateHabitLogDto>
{
    public CreateHabitLogDtoValidator()
    {
        RuleFor(x => x.HabitId)
            .GreaterThan(0).WithMessage("ID привычки должен быть положительным числом");

        RuleFor(x => x.CompletedAt)
            .NotEmpty().WithMessage("Дата выполнения обязательна")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Дата выполнения не может быть в будущем");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Заметки не должны превышать 1000 символов")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
