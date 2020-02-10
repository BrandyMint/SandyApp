using System.Collections.Generic;
using UINotify;

public static partial class Prefs {
    private const string _TXT_SAVED = "Сохранено";
    private const string _TXT_SAVE_FAIL = "Не удалось сохранить";
    private const string _TXT_INCORRECT_DATA = "Некоррестные данные";

    public static void NotifySaved(bool success) {
        if (success) {
            //Notify.Show(Style.SUCCESS, _TXT_SAVED);
        } else {
            Notify.Show(Style.FAIL, _TXT_SAVE_FAIL);
        }
    }
    
    public static void NotifyIncorrectData() {
        Notify.Show(Style.FAIL, $"{_TXT_SAVE_FAIL}. {_TXT_INCORRECT_DATA}");
    }
    
    public static readonly AppParams App = new AppParams();
    
    public class AppParams : SerializableParams {
        public bool FlipHorizontal {
            get => Get(nameof(FlipHorizontal), false);
            set => Set(nameof(FlipHorizontal), value);
        }
        
        public bool FlipVertical {
            get => Get(nameof(FlipVertical), false);
            set => Set(nameof(FlipVertical), value);
        }
        
        public bool FlipHorizontalSandbox {
            get => Get(nameof(FlipHorizontalSandbox), true);
            set => Set(nameof(FlipHorizontalSandbox), value);
        }
        
        public bool FlipVerticalSandbox {
            get => Get(nameof(FlipVerticalSandbox), false);
            set => Set(nameof(FlipVerticalSandbox), value);
        }
    }
    
    public static IEnumerable<SerializableParams> RecordPlayerOverrides() {
        yield return App;
        yield return Projector;
        yield return Calibration;
        yield return Sandbox;
        yield return Landscape;
    }
}