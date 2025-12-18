using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ExpressControls;
using NAudio.Wave;
using Present_Express.Properties;

namespace Present_Express
{
    /// <summary>
    /// Interaction logic for Slideshow.xaml
    /// </summary>
    public partial class Slideshow : ExpressWindow
    {
        private readonly List<Slide> AllSlides;
        private int CurrentSlide = -1;

        private Image PhotoImg;
        private Image PhotoImgOther;
        private Grid PhotoGrid;
        private Grid PhotoGridOther;

        private readonly DispatcherTimer SlideTimer = new()
        {
            Interval = new TimeSpan(0, 0, 0, 2),
            IsEnabled = false,
        };
        private readonly DispatcherTimer MoveTimer = new()
        {
            Interval = new TimeSpan(0, 0, 0, 2),
            IsEnabled = false,
        };

        private Point previousMousePosition;
        private readonly Storyboard TransitionStoryboard = new();
        private bool TransitionRunning = false;

        private readonly bool LoopOn;
        private readonly bool UseTimings;
        private bool IsClosing = false;

        private readonly SlideshowSoundtrack Soundtrack;
        private readonly WaveOutEvent PlaybackDevice = new();
        private WaveStream? PlaybackStream;
        private MemoryStream? PlaybackMemory;
        private int NextTrack = 0;

        public Slideshow(List<Slide> slides, SlideshowInfo info, int monitor, int start = 0)
        {
            InitializeComponent();

            PhotoGrid = PhotoGrid1;
            PhotoGridOther = PhotoGrid2;
            PhotoImg = PhotoImg1;
            PhotoImgOther = PhotoImg2;

            PhotoGrid1.Visibility = Visibility.Collapsed;
            PhotoGrid2.Visibility = Visibility.Collapsed;

            PhotoGrid.Background = info.BackColour;
            PhotoGrid.Width = info.Width;
            PhotoGrid.Height = info.Height;
            PhotoImg.Stretch = info.FitToSlide ? Stretch.Uniform : Stretch.Fill;

            PhotoGridOther.Background = info.BackColour;
            PhotoGridOther.Width = info.Width;
            PhotoGridOther.Height = info.Height;
            PhotoImgOther.Stretch = info.FitToSlide ? Stretch.Uniform : Stretch.Fill;

            LoopOn = info.Loop;
            UseTimings = info.UseTimings;
            Soundtrack = info.Soundtrack;
            AllSlides = slides;
            CurrentSlide = start - 1;

            try
            {
                System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.AllScreens[
                    monitor
                ];
                Top = screen.WorkingArea.Top;
                Left = screen.WorkingArea.Left;
            }
            catch { }

            SlideTimer.Tick += SlideTimer_Tick;
            MoveTimer.Tick += MoveTimer_Tick;
            TransitionStoryboard.Completed += TransitionStoryboard_Completed;
            PlaybackDevice.PlaybackStopped += PlaybackDevice_PlaybackStopped;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Funcs.LogConversion(
                PageID,
                LoggingProperties.Conversion.SlideshowStarted,
                $"Slide {CurrentSlide + 2}"
            );

            WindowState = WindowState.Maximized;
            PlayNextTrack();
            LoadNext();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SlideTimer.Stop();
            MoveTimer.Stop();
            IsClosing = true;

            PlaybackDevice.Dispose();
            PlaybackStream?.Dispose();
            PlaybackMemory?.Dispose();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState != WindowState.Maximized)
                WindowState = WindowState.Maximized;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    break;
                case Key.Left:
                case Key.Up:
                    LoadPrevious();
                    break;
                case Key.Right:
                case Key.Down:
                case Key.Space:
                    if (EndGrid.Visibility == Visibility.Visible)
                        Close();
                    else
                        LoadNext();
                    break;
                case Key.Home:
                    LoadStart();
                    break;
                case Key.End:
                    LoadEnd();
                    break;
                default:
                    break;
            }
        }

        private void Window_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // check if mouse pointer has actually moved
            Point currentMousePosition = e.GetPosition(this);
            if (currentMousePosition == previousMousePosition)
            {
                return;
            }
            previousMousePosition = currentMousePosition;

            Cursor = Cursors.Arrow;
            ButtonStack.Visibility = Visibility.Visible;
            MoveTimer.Stop();

            if (Settings.Default.HideControls)
                MoveTimer.Start();
        }

        #region Change Slide

        public void LoadNext()
        {
            SlideTimer.Stop();
            CurrentSlide++;

            if (TransitionRunning || EndGrid.Visibility == Visibility.Visible)
            {
                TransitionStoryboard.Stop();

                if (CurrentSlide >= AllSlides.Count && !LoopOn)
                {
                    CurrentSlide = AllSlides.Count;
                    EndGrid.Visibility = Visibility.Visible;
                    TransitionRunning = false;
                }
                else
                {
                    PhotoImg.Source = AllSlides[CurrentSlide].Bitmap;
                    PhotoGrid.Visibility = Visibility.Visible;
                    EndGrid.Visibility = Visibility.Collapsed;

                    TransitionFinished();
                }
                return;
            }

            if (CurrentSlide >= AllSlides.Count)
            {
                if (LoopOn)
                    LoadStart();
                else
                {
                    CurrentSlide = AllSlides.Count;
                    EndGrid.Visibility = Visibility.Visible;
                }
            }
            else
            {
                (PhotoImg, PhotoImgOther) = (PhotoImgOther, PhotoImg);
                (PhotoGrid, PhotoGridOther) = (PhotoGridOther, PhotoGrid);

                if (
                    Funcs.GetTransitionCategory(AllSlides[CurrentSlide].Transition.Type)
                    != TransitionCategory.Uncover
                )
                {
                    Panel.SetZIndex(PhotoGrid, 2);
                    Panel.SetZIndex(PhotoGridOther, 1);
                }
                else
                {
                    Panel.SetZIndex(PhotoGrid, 1);
                    Panel.SetZIndex(PhotoGridOther, 2);
                }

                PhotoImg.Source = AllSlides[CurrentSlide].Bitmap;
                PhotoGrid.Visibility = Visibility.Visible;
                EndGrid.Visibility = Visibility.Collapsed;

                LoadTransition();
            }
        }

        public void LoadPrevious()
        {
            if (CurrentSlide > 0)
            {
                CurrentSlide -= 2;
                LoadNext();
            }
            else
            {
                PhotoGrid1.Visibility = Visibility.Collapsed;
                PhotoGrid2.Visibility = Visibility.Collapsed;
                PhotoImg1.Source = null;
                PhotoImg2.Source = null;

                TransitionRunning = false;
                LoadStart();
            }
        }

        public void LoadStart()
        {
            CurrentSlide = -1;
            LoadNext();
        }

        public void LoadEnd()
        {
            CurrentSlide = AllSlides.Count - 2;
            LoadNext();
        }

        private void SlideTimer_Tick(object? sender, EventArgs e)
        {
            SlideTimer.Stop();
            LoadNext();
        }

        #endregion
        #region Transitions

        private void LoadTransition()
        {
            Transition trans = AllSlides[CurrentSlide].Transition;
            TransitionStoryboard.Children.Clear();
            TransitionStoryboard.Stop();

            switch (trans.Type)
            {
                case TransitionType.Fade:
                    DoubleAnimation fadeAnimation = new(0, 1, TimeSpan.FromSeconds(trans.Duration));

                    Storyboard.SetTarget(fadeAnimation, PhotoGrid);
                    Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));

                    TransitionStoryboard.Children.Add(fadeAnimation);
                    break;

                case TransitionType.FadeThroughBlack:
                    DoubleAnimationUsingKeyFrames fadePhase1 = new()
                    {
                        KeyFrames =
                        {
                            new EasingDoubleKeyFrame(
                                1,
                                KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0))
                            ),
                            new EasingDoubleKeyFrame(
                                0,
                                KeyTime.FromTimeSpan(TimeSpan.FromSeconds(trans.Duration / 2))
                            ),
                        },
                    };
                    DoubleAnimationUsingKeyFrames fadePhase2 = new()
                    {
                        KeyFrames =
                        {
                            new EasingDoubleKeyFrame(
                                0,
                                KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0))
                            ),
                            new EasingDoubleKeyFrame(
                                0,
                                KeyTime.FromTimeSpan(TimeSpan.FromSeconds(trans.Duration / 2))
                            ),
                        },
                    };
                    DoubleAnimationUsingKeyFrames fadePhase3 = new()
                    {
                        KeyFrames =
                        {
                            new EasingDoubleKeyFrame(
                                0,
                                KeyTime.FromTimeSpan(TimeSpan.FromSeconds(trans.Duration / 2))
                            ),
                            new EasingDoubleKeyFrame(
                                1,
                                KeyTime.FromTimeSpan(TimeSpan.FromSeconds(trans.Duration))
                            ),
                        },
                    };

                    Storyboard.SetTarget(fadePhase1, PhotoGridOther);
                    Storyboard.SetTargetProperty(fadePhase1, new PropertyPath(OpacityProperty));
                    Storyboard.SetTarget(fadePhase2, PhotoGrid);
                    Storyboard.SetTargetProperty(fadePhase2, new PropertyPath(OpacityProperty));
                    Storyboard.SetTarget(fadePhase3, PhotoGrid);
                    Storyboard.SetTargetProperty(fadePhase3, new PropertyPath(OpacityProperty));

                    TransitionStoryboard.Children.Add(fadePhase1);
                    TransitionStoryboard.Children.Add(fadePhase2);
                    TransitionStoryboard.Children.Add(fadePhase3);
                    break;

                case TransitionType.PushLeft:
                case TransitionType.PushRight:
                case TransitionType.PushTop:
                case TransitionType.PushBottom:
                    double pushInitial = 0,
                        pushFinal = 0;
                    string pushCoord = "X";

                    switch ((TransitionDirection)Funcs.GetTransitionInc(trans.Type))
                    {
                        case TransitionDirection.Left:
                            pushInitial = PhotoGrid.Width;
                            pushFinal = -PhotoGrid.Width;
                            break;
                        case TransitionDirection.Right:
                            pushInitial = -PhotoGrid.Width;
                            pushFinal = PhotoGrid.Width;
                            break;
                        case TransitionDirection.Top:
                            pushInitial = PhotoGrid.Height;
                            pushFinal = -PhotoGrid.Height;
                            pushCoord = "Y";
                            break;
                        case TransitionDirection.Bottom:
                            pushInitial = -PhotoGrid.Height;
                            pushFinal = PhotoGrid.Height;
                            pushCoord = "Y";
                            break;
                        default:
                            break;
                    }

                    DoubleAnimationUsingKeyFrames pushAnimation1 = new()
                    {
                        KeyFrames =
                        {
                            new EasingDoubleKeyFrame(
                                0,
                                KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0))
                            ),
                            new EasingDoubleKeyFrame(
                                pushInitial,
                                KeyTime.FromTimeSpan(TimeSpan.FromSeconds(trans.Duration))
                            ),
                        },
                    };
                    DoubleAnimationUsingKeyFrames pushAnimation2 = new()
                    {
                        KeyFrames =
                        {
                            new EasingDoubleKeyFrame(
                                pushFinal,
                                KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0))
                            ),
                            new EasingDoubleKeyFrame(
                                0,
                                KeyTime.FromTimeSpan(TimeSpan.FromSeconds(trans.Duration))
                            ),
                        },
                    };

                    Storyboard.SetTarget(pushAnimation1, PhotoGridOther);
                    Storyboard.SetTargetProperty(
                        pushAnimation1,
                        new PropertyPath(
                            $"(UIElement.RenderTransform).(TransformGroup.Children)[3].(TranslateTransform.{pushCoord})"
                        )
                    );
                    Storyboard.SetTarget(pushAnimation2, PhotoGrid);
                    Storyboard.SetTargetProperty(
                        pushAnimation2,
                        new PropertyPath(
                            $"(UIElement.RenderTransform).(TransformGroup.Children)[3].(TranslateTransform.{pushCoord})"
                        )
                    );

                    TransitionStoryboard.Children.Add(pushAnimation1);
                    TransitionStoryboard.Children.Add(pushAnimation2);
                    break;

                case TransitionType.WipeLeft:
                case TransitionType.WipeRight:
                case TransitionType.WideTop:
                case TransitionType.WideBottom:
                    switch ((TransitionDirection)Funcs.GetTransitionInc(trans.Type))
                    {
                        case TransitionDirection.Left:
                            ((LinearGradientBrush)PhotoGrid.OpacityMask).StartPoint = new Point(
                                0,
                                0
                            );
                            ((LinearGradientBrush)PhotoGrid.OpacityMask).EndPoint = new Point(1, 0);
                            break;
                        case TransitionDirection.Right:
                            ((LinearGradientBrush)PhotoGrid.OpacityMask).StartPoint = new Point(
                                1,
                                0
                            );
                            ((LinearGradientBrush)PhotoGrid.OpacityMask).EndPoint = new Point(0, 0);
                            break;
                        case TransitionDirection.Top:
                            ((LinearGradientBrush)PhotoGrid.OpacityMask).StartPoint = new Point(
                                0,
                                0
                            );
                            ((LinearGradientBrush)PhotoGrid.OpacityMask).EndPoint = new Point(0, 1);
                            break;
                        case TransitionDirection.Bottom:
                            ((LinearGradientBrush)PhotoGrid.OpacityMask).StartPoint = new Point(
                                0,
                                1
                            );
                            ((LinearGradientBrush)PhotoGrid.OpacityMask).EndPoint = new Point(0, 0);
                            break;
                        default:
                            break;
                    }

                    DoubleAnimationUsingKeyFrames wipeAnimation1 = new()
                    {
                        KeyFrames =
                        {
                            new EasingDoubleKeyFrame(
                                0,
                                KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0))
                            ),
                            new EasingDoubleKeyFrame(
                                1,
                                KeyTime.FromTimeSpan(TimeSpan.FromSeconds(trans.Duration))
                            ),
                        },
                    };
                    DoubleAnimationUsingKeyFrames wipeAnimation2 = new()
                    {
                        KeyFrames =
                        {
                            new EasingDoubleKeyFrame(
                                0,
                                KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0))
                            ),
                            new EasingDoubleKeyFrame(
                                1,
                                KeyTime.FromTimeSpan(TimeSpan.FromSeconds(trans.Duration))
                            ),
                        },
                    };

                    Storyboard.SetTarget(wipeAnimation1, PhotoGrid);
                    Storyboard.SetTargetProperty(
                        wipeAnimation1,
                        new PropertyPath(
                            "(UIElement.OpacityMask).(GradientBrush.GradientStops)[0].(GradientStop.Offset)"
                        )
                    );
                    Storyboard.SetTarget(wipeAnimation2, PhotoGrid);
                    Storyboard.SetTargetProperty(
                        wipeAnimation2,
                        new PropertyPath(
                            "(UIElement.OpacityMask).(GradientBrush.GradientStops)[1].(GradientStop.Offset)"
                        )
                    );

                    TransitionStoryboard.Children.Add(wipeAnimation1);
                    TransitionStoryboard.Children.Add(wipeAnimation2);
                    break;

                case TransitionType.UncoverLeft:
                case TransitionType.UncoverRight:
                case TransitionType.UncoverTop:
                case TransitionType.UncoverBottom:
                    double uncoverValue = 0;
                    string uncoverCoord = "X";

                    switch ((TransitionDirection)Funcs.GetTransitionInc(trans.Type))
                    {
                        case TransitionDirection.Left:
                            uncoverValue = PhotoGrid.Width;
                            break;
                        case TransitionDirection.Right:
                            uncoverValue = -PhotoGrid.Width;
                            break;
                        case TransitionDirection.Top:
                            uncoverValue = PhotoGrid.Height;
                            uncoverCoord = "Y";
                            break;
                        case TransitionDirection.Bottom:
                            uncoverValue = -PhotoGrid.Height;
                            uncoverCoord = "Y";
                            break;
                        default:
                            break;
                    }

                    DoubleAnimationUsingKeyFrames uncoverAnimation = new()
                    {
                        KeyFrames =
                        {
                            new EasingDoubleKeyFrame(
                                0,
                                KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0))
                            ),
                            new EasingDoubleKeyFrame(
                                uncoverValue,
                                KeyTime.FromTimeSpan(TimeSpan.FromSeconds(trans.Duration))
                            ),
                        },
                    };

                    Storyboard.SetTarget(uncoverAnimation, PhotoGridOther);
                    Storyboard.SetTargetProperty(
                        uncoverAnimation,
                        new PropertyPath(
                            $"(UIElement.RenderTransform).(TransformGroup.Children)[3].(TranslateTransform.{uncoverCoord})"
                        )
                    );

                    TransitionStoryboard.Children.Add(uncoverAnimation);
                    break;

                case TransitionType.CoverLeft:
                case TransitionType.CoverRight:
                case TransitionType.CoverTop:
                case TransitionType.CoverBottom:
                    double coverValue = 0;
                    string coverCoord = "X";

                    switch ((TransitionDirection)Funcs.GetTransitionInc(trans.Type))
                    {
                        case TransitionDirection.Left:
                            coverValue = -PhotoGrid.Width;
                            break;
                        case TransitionDirection.Right:
                            coverValue = PhotoGrid.Width;
                            break;
                        case TransitionDirection.Top:
                            coverValue = -PhotoGrid.Height;
                            coverCoord = "Y";
                            break;
                        case TransitionDirection.Bottom:
                            coverValue = PhotoGrid.Height;
                            coverCoord = "Y";
                            break;
                        default:
                            break;
                    }

                    DoubleAnimationUsingKeyFrames coverAnimation = new()
                    {
                        KeyFrames =
                        {
                            new EasingDoubleKeyFrame(
                                coverValue,
                                KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0))
                            ),
                            new EasingDoubleKeyFrame(
                                0,
                                KeyTime.FromTimeSpan(TimeSpan.FromSeconds(trans.Duration))
                            ),
                        },
                    };

                    Storyboard.SetTarget(coverAnimation, PhotoGrid);
                    Storyboard.SetTargetProperty(
                        coverAnimation,
                        new PropertyPath(
                            $"(UIElement.RenderTransform).(TransformGroup.Children)[3].(TranslateTransform.{coverCoord})"
                        )
                    );

                    TransitionStoryboard.Children.Add(coverAnimation);
                    break;

                case TransitionType.None:
                default:
                    TransitionFinished();
                    return;
            }

            TransitionStoryboard.Begin();
            TransitionRunning = true;
        }

        private void TransitionStoryboard_Completed(object? sender, EventArgs e)
        {
            TransitionFinished();
        }

        private void TransitionFinished()
        {
            TransitionRunning = false;
            PhotoGridOther.Visibility = Visibility.Collapsed;

            if (UseTimings)
            {
                SlideTimer.Interval = TimeSpan.FromSeconds(AllSlides[CurrentSlide].Timing);
                SlideTimer.Start();
            }
        }

        #endregion
        #region Grid Clicks

        private void EndGrid_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                Close();
        }

        private void PhotoGrid_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                LoadNext();
        }

        #endregion
        #region Controls

        public void PrevBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadPrevious();
        }

        public void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (EndGrid.Visibility == Visibility.Collapsed)
                LoadNext();
            else
                Close();
        }

        public void HomeBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadStart();
        }

        public void EndBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ButtonStack_MouseEnter(object sender, MouseEventArgs e)
        {
            ButtonStack.Opacity = 1;
        }

        private void ButtonStack_MouseLeave(object sender, MouseEventArgs e)
        {
            ButtonStack.Opacity = 0.4;
        }

        private void MoveTimer_Tick(object? sender, EventArgs e)
        {
            if (ButtonStack.Opacity < 1)
            {
                MoveTimer.Stop();
                Cursor = Cursors.None;
                ButtonStack.Visibility = Visibility.Collapsed;
            }
        }

        #endregion
        #region Soundtrack

        private void PlayNextTrack()
        {
            if (Soundtrack.Filenames.Count > 0)
            {
                if (NextTrack >= Soundtrack.Filenames.Count)
                {
                    if (Soundtrack.Loop)
                        NextTrack = 0;
                    else
                        return; // No more tracks to play
                }

                try
                {
                    string trackName = Soundtrack.Filenames[NextTrack];
                    PlaybackMemory = new(Soundtrack.Audio[trackName]);

                    if (
                        Path.GetExtension(trackName)
                            .Equals(".wav", StringComparison.OrdinalIgnoreCase)
                    )
                        PlaybackStream = new WaveFileReader(PlaybackMemory);
                    else
                        PlaybackStream = new Mp3FileReader(PlaybackMemory);

                    PlaybackDevice.Init(PlaybackStream);
                    PlaybackDevice.Play();
                }
                catch
                {
                    PlayNextTrack();
                }
                finally
                {
                    NextTrack++;
                }
            }
        }

        private void PlaybackDevice_PlaybackStopped(object? sender, StoppedEventArgs e)
        {
            if (!IsClosing)
                PlayNextTrack();
        }

        #endregion
    }
}
