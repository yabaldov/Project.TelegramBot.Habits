using HabiBot.Application.Services;
using HabiBot.Infrastructure.Models.Telegram;

namespace HabiBot.Infrastructure.Services;

/// <summary>
/// Вспомогательный класс для построения inline keyboard из сводки
/// </summary>
public static class SummaryKeyboardBuilder
{
    /// <summary>
    /// Построить inline keyboard из невыполненных привычек для быстрой отметки
    /// </summary>
    public static InlineKeyboardMarkup? BuildUncompletedHabitsKeyboard(DailySummaryData summary)
    {
        if (!summary.UncompletedHabits.Any())
        {
            return null;
        }

        var buttons = summary.UncompletedHabits
            .OrderBy(h => h.ScheduledTime)
            .Select(habit => new List<InlineKeyboardButton>
            {
                new() { Text = $"✅ {habit.HabitName}", CallbackData = $"summarycomplete:{habit.HabitId}" }
            })
            .ToList();

        return new InlineKeyboardMarkup { InlineKeyboard = buttons };
    }
}
