using HabiBot.Application.Services;
using HabiBot.Bot.StateManagement;
using HabiBot.Infrastructure.Models.Telegram;
using HabiBot.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HabiBot.Bot.Commands;

/// <summary>
/// Команда /edit - редактирование привычки
/// </summary>
public class EditCommand : BotCommandBase
{
    private readonly IUserService _userService;
    private readonly IHabitService _habitService;

    public EditCommand(
        ITelegramApiClient telegramClient,
        IUserStateManager stateManager,
        IUserService userService,
        IHabitService habitService,
        ILogger<EditCommand> logger)
        : base(telegramClient, stateManager, logger)
    {
        _userService = userService;
        _habitService = habitService;
    }

    public override string Name => "edit";
    public override string Description => "Редактировать привычку";

    public override async Task ExecuteAsync(Update update, CancellationToken cancellationToken = default)
    {
        var chatId = GetChatId(update);
        var userId = update.Message?.From?.Id;

        if (chatId == null || userId == null)
        {
            Logger.LogWarning("Получен Update без ChatId или UserId");
            return;
        }

        try
        {
            // Проверяем, зарегистрирован ли пользователь
            var user = await _userService.GetByTelegramIdAsync(userId.Value, cancellationToken);
            if (user == null)
            {
                await SendMessageAsync(chatId.Value,
                    "Вы не зарегистрированы. Используйте /start для регистрации.",
                    cancellationToken);
                return;
            }

            // Получаем список привычек
            var habits = (await _habitService.GetUserHabitsAsync(user.Id, cancellationToken)).ToList();

            if (habits.Count == 0)
            {
                await SendMessageAsync(chatId.Value,
                    "У вас пока нет привычек. Добавьте привычку командой /add",
                    cancellationToken);
                return;
            }

            // Создаем inline клавиатуру с привычками
            var keyboard = new InlineKeyboardMarkup
            {
                InlineKeyboard = habits.Select(h => new[]
                {
                    new InlineKeyboardButton
                    {
                        Text = h.Name,
                        CallbackData = $"edit:{h.Id}"
                    }
                })
            };

            await SendMessageAsync(chatId.Value,
                "Выберите привычку для редактирования:",
                cancellationToken,
                replyMarkup: keyboard);

            Logger.LogInformation("Показан список привычек для редактирования пользователю {UserId}", userId.Value);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка выполнения команды /edit для чата {ChatId}", chatId);
            await SendMessageAsync(chatId.Value,
                "Произошла ошибка при получении списка привычек.",
                cancellationToken);
        }
    }

    /// <summary>
    /// Обработка выбора привычки для редактирования
    /// </summary>
    public async Task HandleHabitSelectedAsync(long chatId, long userId, long habitId, CancellationToken cancellationToken = default)
    {
        try
        {
            var habit = await _habitService.GetByIdAsync(habitId, cancellationToken);

            if (habit == null || habit.UserId != (await _userService.GetByTelegramIdAsync(userId, cancellationToken))?.Id)
            {
                await SendMessageAsync(chatId, "Привычка не найдена.", cancellationToken);
                return;
            }

            // Сохраняем ID привычки в данных состояния
            StateManager.SetData(chatId, "EditHabitId", habitId.ToString());
            StateManager.SetState(chatId, UserState.WaitingForEditField);

            // Создаем клавиатуру для выбора поля
            var keyboard = new InlineKeyboardMarkup
            {
                InlineKeyboard = new[]
                {
                    new[]
                    {
                        new InlineKeyboardButton
                        {
                            Text = "📝 Изменить название",
                            CallbackData = $"editfield:name:{habitId}"
                        }
                    },
                    new[]
                    {
                        new InlineKeyboardButton
                        {
                            Text = "⏰ Изменить время",
                            CallbackData = $"editfield:time:{habitId}"
                        }
                    },
                    new[]
                    {
                        new InlineKeyboardButton
                        {
                            Text = "📅 Изменить частоту",
                            CallbackData = $"editfield:frequency:{habitId}"
                        }
                    },
                    new[]
                    {
                        new InlineKeyboardButton
                        {
                            Text = "❌ Отмена",
                            CallbackData = "editfield:cancel"
                        }
                    }
                }
            };

            var info = $"**Текущие параметры привычки:**\n\n" +
                      $"📌 Название: {habit.Name}\n" +
                      $"⏰ Время: {habit.ReminderTime}\n" +
                      $"📅 Частота: {GetFrequencyText(habit.Frequency)}\n\n" +
                      $"Что вы хотите изменить?";

            await SendMessageAsync(chatId, info, cancellationToken, replyMarkup: keyboard);

            Logger.LogInformation("Показаны параметры привычки {HabitId} для редактирования", habitId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка при отображении параметров привычки {HabitId}", habitId);
            await SendMessageAsync(chatId, "Произошла ошибка.", cancellationToken);
            StateManager.ClearState(chatId);
        }
    }

    /// <summary>
    /// Обработка выбора поля для редактирования
    /// </summary>
    public async Task HandleFieldSelectedAsync(long chatId, long userId, string field, long habitId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (field == "cancel")
            {
                StateManager.ClearState(chatId);
                await SendMessageAsync(chatId, "Редактирование отменено.", cancellationToken);
                return;
            }

            StateManager.SetData(chatId, "EditHabitId", habitId.ToString());
            StateManager.SetData(chatId, "EditField", field);

            switch (field)
            {
                case "name":
                    StateManager.SetState(chatId, UserState.WaitingForEditName);
                    await SendMessageAsync(chatId, "Введите новое название привычки:", cancellationToken);
                    break;

                case "time":
                    StateManager.SetState(chatId, UserState.WaitingForEditTime);
                    await SendMessageAsync(chatId, "Введите новое время напоминания (формат: HH:mm, например 09:00):", cancellationToken);
                    break;

                case "frequency":
                    var keyboard = new InlineKeyboardMarkup
                    {
                        InlineKeyboard = new[]
                        {
                            new[]
                            {
                                new InlineKeyboardButton
                                {
                                    Text = "Ежедневно",
                                    CallbackData = $"frequency:Daily:{habitId}"
                                }
                            },
                            new[]
                            {
                                new InlineKeyboardButton
                                {
                                    Text = "Еженедельно",
                                    CallbackData = $"frequency:Weekly:{habitId}"
                                }
                            },
                            new[]
                            {
                                new InlineKeyboardButton
                                {
                                    Text = "Произвольная",
                                    CallbackData = $"frequency:Custom:{habitId}"
                                }
                            }
                        }
                    };
                    await SendMessageAsync(chatId, "Выберите новую частоту выполнения:", cancellationToken, replyMarkup: keyboard);
                    break;
            }

            Logger.LogInformation("Начато редактирование поля {Field} привычки {HabitId}", field, habitId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка при выборе поля для редактирования");
            await SendMessageAsync(chatId, "Произошла ошибка.", cancellationToken);
            StateManager.ClearState(chatId);
        }
    }

    /// <summary>
    /// Обработка ввода нового названия
    /// </summary>
    public async Task HandleNameInputAsync(long chatId, long userId, string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var habitIdStr = StateManager.GetData<string>(chatId, "EditHabitId");
            if (string.IsNullOrEmpty(habitIdStr) || !long.TryParse(habitIdStr, out var habitId))
            {
                await SendMessageAsync(chatId, "Произошла ошибка. Попробуйте снова с /edit", cancellationToken);
                StateManager.ClearState(chatId);
                return;
            }

            var habit = await _habitService.GetByIdAsync(habitId, cancellationToken);
            if (habit == null)
            {
                await SendMessageAsync(chatId, "Привычка не найдена.", cancellationToken);
                StateManager.ClearState(chatId);
                return;
            }

            var updateDto = new Application.DTOs.UpdateHabitDto
            {
                Id = habitId,
                Name = name
            };

            await _habitService.UpdateHabitAsync(updateDto, cancellationToken);

            StateManager.ClearState(chatId);
            await SendMessageAsync(chatId, $"✅ Название привычки изменено на: **{name}**", cancellationToken);

            Logger.LogInformation("Название привычки {HabitId} изменено на {NewName}", habitId, name);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка при изменении названия привычки");
            await SendMessageAsync(chatId, "Произошла ошибка при изменении названия.", cancellationToken);
            StateManager.ClearState(chatId);
        }
    }

    /// <summary>
    /// Обработка ввода нового времени
    /// </summary>
    public async Task HandleTimeInputAsync(long chatId, long userId, string time, CancellationToken cancellationToken = default)
    {
        try
        {
            var habitIdStr = StateManager.GetData<string>(chatId, "EditHabitId");
            if (string.IsNullOrEmpty(habitIdStr) || !long.TryParse(habitIdStr, out var habitId))
            {
                await SendMessageAsync(chatId, "Произошла ошибка. Попробуйте снова с /edit", cancellationToken);
                StateManager.ClearState(chatId);
                return;
            }

            var habit = await _habitService.GetByIdAsync(habitId, cancellationToken);
            if (habit == null)
            {
                await SendMessageAsync(chatId, "Привычка не найдена.", cancellationToken);
                StateManager.ClearState(chatId);
                return;
            }

            var updateDto = new Application.DTOs.UpdateHabitDto
            {
                Id = habitId,
                ReminderTime = time
            };

            await _habitService.UpdateHabitAsync(updateDto, cancellationToken);

            StateManager.ClearState(chatId);
            await SendMessageAsync(chatId, $"✅ Время напоминания изменено на: **{time}**", cancellationToken);

            Logger.LogInformation("Время привычки {HabitId} изменено на {NewTime}", habitId, time);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка при изменении времени привычки");
            await SendMessageAsync(chatId, "Произошла ошибка при изменении времени. Проверьте формат (HH:mm).", cancellationToken);
            StateManager.ClearState(chatId);
        }
    }

    /// <summary>
    /// Обработка выбора новой частоты
    /// </summary>
    public async Task HandleFrequencySelectedAsync(long chatId, long userId, string frequency, long habitId, CancellationToken cancellationToken = default)
    {
        try
        {
            var habit = await _habitService.GetByIdAsync(habitId, cancellationToken);
            if (habit == null)
            {
                await SendMessageAsync(chatId, "Привычка не найдена.", cancellationToken);
                StateManager.ClearState(chatId);
                return;
            }

            var frequencyEnum = Enum.Parse<Domain.Enums.HabitFrequency>(frequency);

            var updateDto = new Application.DTOs.UpdateHabitDto
            {
                Id = habitId,
                Frequency = frequencyEnum
            };

            await _habitService.UpdateHabitAsync(updateDto, cancellationToken);

            StateManager.ClearState(chatId);
            await SendMessageAsync(chatId, $"✅ Частота выполнения изменена на: **{GetFrequencyText(frequencyEnum)}**", cancellationToken);

            Logger.LogInformation("Частота привычки {HabitId} изменена на {NewFrequency}", habitId, frequency);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка при изменении частоты привычки");
            await SendMessageAsync(chatId, "Произошла ошибка при изменении частоты.", cancellationToken);
            StateManager.ClearState(chatId);
        }
    }

    private static string GetFrequencyText(Domain.Enums.HabitFrequency frequency) => frequency switch
    {
        Domain.Enums.HabitFrequency.Daily => "Ежедневно",
        Domain.Enums.HabitFrequency.Weekly => "Еженедельно",
        Domain.Enums.HabitFrequency.Custom => "Произвольная",
        _ => frequency.ToString()
    };
}
