using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using PCL2.Neo.Animations;
using PCL2.Neo.Animations.Easings;
using PCL2.Neo.Helpers;
using System;
using System.Threading.Tasks;

namespace PCL2.Neo.Controls
{
    [PseudoClasses(":unloaded", ":loading", ":stopped", ":error")]
    public class MyLoading : TemplatedControl
    {
        private AnimationHelper _animation;
        private Path? _pathPickaxe;
        private Path? _pathError;
        private Path? _pathLeft;
        private Path? _pathRight;
        private bool _hasErrorOccurred = false;

        public MyLoading()
        {
            _animation = new();
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            _pathPickaxe = e.NameScope.Find<Path>("PathPickaxe");
            _pathError = e.NameScope.Find<Path>("PathError");
            _pathLeft = e.NameScope.Find<Path>("PathLeft");
            _pathRight = e.NameScope.Find<Path>("PathRight");

            SetPseudoClasses();

            StartAnimation();
        }

        public enum LoadingState
        {
            Unloaded = -1,
            Loading = 0,
            Stopped = 1,
            Error = 2
        }

        public static readonly StyledProperty<LoadingState> StateProperty = AvaloniaProperty.Register<MyLoading, LoadingState>(
            nameof(State), LoadingState.Loading);

        public LoadingState State
        {
            get => GetValue(StateProperty);
            set
            {
                SetValue(StateProperty, value);
                SetPseudoClasses();
            }
        }

        private void StartAnimation()
        {
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                while (true)
                {
                    var currentState = State;
                    switch (currentState)
                    {
                        case LoadingState.Loading:
                            if (_hasErrorOccurred)
                            {
                                await AnimationErrorToLoadingAsync();
                            }
                            _hasErrorOccurred = false;
                            await AnimationLoadingAsync();
                            break;
                        case LoadingState.Error:
                            if (!_hasErrorOccurred)
                            {
                                _hasErrorOccurred = true;
                                await AnimationLoadingToErrorAsync();
                                break;
                            }
                            await Task.Delay(100);
                            break;
                        default:
                            await Task.Delay(100);
                            break;
                    }
                }
            });
        }

        private async Task AnimationErrorToLoadingAsync()
        {
            _animation.CancelAndClear();
            _animation.Animations.AddRange(
            [
                new RotateTransformAngleAnimation(this._pathPickaxe!, TimeSpan.FromMilliseconds(350), 55d, -20d, new MyBackEaseIn(EasePower.Weak)),
                new OpacityAnimation(this._pathError!, TimeSpan.FromMilliseconds(100), 0d),
                new ScaleTransformScaleXAnimation(this._pathError!, TimeSpan.FromMilliseconds(100), 1d, 0.5d),
                new ScaleTransformScaleYAnimation(this._pathError!, TimeSpan.FromMilliseconds(400), 1d, 0.5d)
            ]);
            await _animation.RunAsync();
        }

        private async Task AnimationLoadingToErrorAsync()
        {
            _animation.CancelAndClear();
            _animation.Animations.AddRange(
            [
                new RotateTransformAngleAnimation(this._pathPickaxe!, TimeSpan.FromMilliseconds(900), 55d, new CubicEaseOut()),
                new OpacityAnimation(this._pathError!, TimeSpan.FromMilliseconds(300), 1d),
                new ScaleTransformScaleXAnimation(this._pathError!, TimeSpan.FromMilliseconds(400), 0.5d, 1d, new MyBackEaseOut()),
                new ScaleTransformScaleYAnimation(this._pathError!, TimeSpan.FromMilliseconds(400), 0.5d, 1d, new MyBackEaseOut())
            ]);
            await _animation.RunAsync();
        }

        private async Task AnimationLoadingAsync()
        {
            // 循环动画，听说这里折磨龙猫很久(doge)
            _animation.CancelAndClear();
            _animation.Animations.AddRange(
            [
                new RotateTransformAngleAnimation(this._pathPickaxe!, TimeSpan.FromMilliseconds(350), 55d, -20d, new MyBackEaseIn(EasePower.Weak)),
                new RotateTransformAngleAnimation(this._pathPickaxe!, TimeSpan.FromMilliseconds(900), 30d, 55d, new ElasticEaseOut()),
                new RotateTransformAngleAnimation(this._pathPickaxe!, TimeSpan.FromMilliseconds(180), -20d, 30d),
                new OpacityAnimation(this._pathLeft!, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(50), 1d, 0d),
                new XAnimation(this._pathLeft!, TimeSpan.FromMilliseconds(180), -5d, new CubicEaseOut()),
                new YAnimation(this._pathLeft!, TimeSpan.FromMilliseconds(180), -6d, new CubicEaseOut()),
                new OpacityAnimation(this._pathRight!, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(50), 1d, 0d),
                new XAnimation(this._pathRight!, TimeSpan.FromMilliseconds(180), 5d, new CubicEaseOut()),
                new YAnimation(this._pathRight!, TimeSpan.FromMilliseconds(180), -6d, new CubicEaseOut()),
            ]);
            await _animation.RunAsync();
            this._pathLeft!.Margin = new Thickness(7,41,0,0);
            this._pathRight!.Margin = new Thickness(14,41,0,0);
        }

        private void SetPseudoClasses()
        {
            PseudoClasses.Remove(":unloaded");
            PseudoClasses.Remove(":loading");
            PseudoClasses.Remove(":stopped");
            PseudoClasses.Remove(":error");
            switch (State)
            {
                case LoadingState.Unloaded:
                    PseudoClasses.Set(":unloaded", true);
                    break;
                case LoadingState.Loading:
                    PseudoClasses.Set(":loading", true);
                    break;
                case LoadingState.Stopped:
                    PseudoClasses.Set(":stopped", true);
                    break;
                case LoadingState.Error:
                    PseudoClasses.Set(":error", true);
                    break;
            }
        }
    }
}