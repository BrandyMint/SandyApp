
using UINotify;

public static partial class Prefs {
    private const string _TXT_SAVED = "Сохранено";
    private const string _TXT_SAVE_FAIL = "Не удалось сохранить";

    public static void NotifySaved(bool success) {
        if (success) {
            Notify.Show(Style.SUCCESS, _TXT_SAVED);
        } else {
            Notify.Show(Style.FAIL, _TXT_SAVE_FAIL);
        }
    }
}