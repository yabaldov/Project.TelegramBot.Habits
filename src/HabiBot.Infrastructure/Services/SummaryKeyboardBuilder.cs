using HabiBot.Application.Services;
using HabiBot.Infrastructure.Models.Telegram;

namespace HabiBot.Infrastructure.Services;

/// <summary>
/// Вспомогательный класс для построения inline keyboard из сводки
/// </summary>
public static class SummaryKeyboardBuilder
{
    /// <summary>
    /// Построить inline keyboard из невыполненных и запланированных привычек для быстрой отметки
    /// </summary>
    public static InlineKeyboardMarkup? BuildUncompletedHabitsKeyboard(DailySummaryData summary)
    {
        var habitsForButtons = summary.AvailableToCompleteToday
            .Where(h => summary.UncompletedHabits.Any(u => u.HabitId == h.HabitId)
                     || summary.ScheduledHabits.Any(s => s.HabitId == h.HabitId))
            .ToList();

        if (!habitsForButtons.Any())
        {
            return null;
        }

        var buttons = habitsForButtons
            .OrderBy(h => h.ScheduledTime)
            .Select(habit => new List<InlineKeyboardButton>
            {
                new() { Text = $"✅ {habit.HabitName}", CallbackData = $"summarycomplete:{habit.HabitId}" }
            })
            .ToList();

        return new InlineKeyboardMarkup { InlineKeyboard = buttons };
    }
}
