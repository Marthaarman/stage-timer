using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Clock_and_countdown
{
    public partial class Form1 : Form
    {
        private readonly Screen[] _screens = Screen.AllScreens;
        private int _backgroundWorkCounter;

        private BackgroundWorker _backgroundWorker1;

        private int _countdownHours, _countdownMinutes, _countdownSeconds;
        private bool _countdownOvertime;

        private bool _countdownStarted;

        private float _factorPreviewProgram = 1;

        private int _fontSize = 35;
        private Form _formOutput;

        private Mode _modeOutput = Mode.Clock;
        private int _programHours, _programMinutes, _programSeconds;
        private Screen _screenSelectedOutputScreen;

        private bool _secondsClock, _secondsCountdown = true;

        public Form1()
        {
            InitializeComponent();

            //  center texts
            ActionCentreTextSettingsForm();


            //  detect _screens
            ActionDetectScreens();

            //  add resize actions
            Resize += ListenerSettingsFormResize;

            //  close output screen on escape button
            KeyUp += ListenerDetectEscape;

            //  action to font size change event
            numericUpDown_fontSize.ValueChanged += ListenerChangeFontSize;


            //  detect change Mode
            radioButton_mode_clock.CheckedChanged += ListenerChangeMode;
            radioButton_mode_countdown.CheckedChanged += ListenerChangeMode;
            radioButton_mode_idle_black.CheckedChanged += ListenerChangeMode;

            //  detect seconds view change
            checkBox_clock_seconds.CheckedChanged += ListenerChangeSecondsClock;
            checkBox_countdown_seconds.CheckedChanged += ListenerChangeSecondsCountdown;


            //  detect change of countdown
            numericUpDown_hours.ValueChanged += ListenerChangeDuration;
            numericUpDown_minutes.ValueChanged += ListenerChangeDuration;
            numericUpDown_seconds.ValueChanged += ListenerChangeDuration;
            dateTimePicker_countdownTime.ValueChanged += ListenerChangeDuration;

            //  set countdown timer format
            dateTimePicker_countdownTime.Format = DateTimePickerFormat.Time;
            dateTimePicker_countdownTime.ShowUpDown = true;

            radioButton_countdownTime.CheckedChanged += ListenerChangeCountdownMode;

            //  backgroundworker 
            //  foor loop every second
            InitializeBackgroundWorker();
        }

        /*-----------------------------------------------------------------------*/
        //  input changes & listeners
        /*-----------------------------------------------------------------------*/

        /// <summary>
        /// This function should be called when the checkbox of showing seconds on the normal clock changes value
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListenerChangeSecondsClock(object sender, EventArgs e)
        {
            _secondsClock = checkBox_clock_seconds.Checked;
        }

        /// <summary>
        /// This function should be called when the checkbox of showing seconds on the countdown timer changes value
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListenerChangeSecondsCountdown(object sender, EventArgs e)
        {
            _secondsCountdown = checkBox_countdown_seconds.Checked;
            ActionSetCountdownTimers();
        }

        /// <summary>
        /// Once the countdown duration has been changed for either hours, minutes or seconds this function must be called
        /// The new timers will be prepared
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListenerChangeDuration(object sender, EventArgs e)
        {
            ActionSetCountdownTimers();
        }

        /// <summary>
        /// When the mode changes between clock or countdown, this function must be called.
        /// The output will be prepared
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListenerChangeMode(object sender, EventArgs e)
        {
            if (radioButton_mode_idle_black.Checked)
            {
                _modeOutput = Mode.Idle;
                ActionPauseCountdown();
            }
            else if (radioButton_mode_countdown.Checked)
            {
                _modeOutput = Mode.Countdown;
                ActionSetCountdownMode();
            }
            else
            {
                ActionPauseCountdown();
                _modeOutput = Mode.Clock;
                ActionSetCountdownTimers();
            }
        }

        /// <summary>
        /// When a switch is made from countdown until a certain time or for a duration, this function must be called to prepare the output
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListenerChangeCountdownMode(object sender, EventArgs e)
        {
            if (_modeOutput == Mode.Countdown) ActionSetCountdownMode();
        }

        /// <summary>
        /// When the form with controls is resized, this function must be called to change output preview accordingly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListenerSettingsFormResize(object sender, EventArgs e)
        {
            ActionCentreTextSettingsForm();
        }

        /// <summary>
        /// When the escape button is pressed, this function should be called to clode the output view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListenerDetectEscape(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Escape)
            {
                return;
            }
            
            e.Handled = true;
            ActionCloseOutputForm();
            
        }

        /// <summary>
        /// When the font size is changed in the controls, this function must be called to change font sizes in the output and preview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListenerChangeFontSize(object sender, EventArgs e)
        {
            ActionTakeFontSize();
            ActionCentreTextSettingsForm();
        }

        /// <summary>
        /// When the output window is resized, this function is called to handle font formats
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListenerOutputFormResize(object sender, EventArgs e)
        {
            ActionCentreTextOutputForm();
        }

        /*-----------------------------------------------------------------------*/
        //  Screen settings
        /*-----------------------------------------------------------------------*/

        /// <summary>
        /// This function is called upon initializing to detect which screens are connected to the machine.
        /// It will select the lastly found screen as output monitor
        /// </summary>
        private void ActionDetectScreens()
        {
            _screenSelectedOutputScreen = _screens[_screens.Length - 1];
            foreach (Screen screen in _screens) comboBox_outputMonitor.Items.Add(screen.DeviceName);
            comboBox_outputMonitor.SelectedIndex = _screens.Length - 1;
            comboBox_outputMonitor.SelectedIndexChanged += ListenerSelectedScreenChanged;
        }

        /// <summary>
        /// When the selected screen is changed in controls, this function will prepare the output for that selected screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListenerSelectedScreenChanged(object sender, EventArgs e)
        {
            foreach (Screen screen in _screens)
                if (screen.DeviceName == comboBox_outputMonitor.SelectedItem.ToString())
                {
                    _screenSelectedOutputScreen = screen;
                    return;
                }

            _screenSelectedOutputScreen = _screens[_screens.Length - 1];
        }

        /*-----------------------------------------------------------------------*/
        //  Backgroundworker
        /*-----------------------------------------------------------------------*/

        /// <summary>
        /// The backgroundworker will be present to tick every second in the background
        /// </summary>
        private void InitializeBackgroundWorker()
        {
            _backgroundWorker1 = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _backgroundWorker1.DoWork += BackgroundWorker1DoWork;
            _backgroundWorker1.RunWorkerCompleted += BackgroundWorker1RunWorkerCompleted;
            _backgroundWorker1.ProgressChanged += BackgroundWorker1ProgressChanged;
            _backgroundWorker1.RunWorkerAsync();
        }

        /// <summary>
        /// The function that does the work. By reporting the progress a function in the foreground will be triggered
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorker1DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                _backgroundWorker1.ReportProgress(0, _backgroundWorkCounter.ToString());
                Thread.Sleep(1000);
                _backgroundWorkCounter++;
            }
        }

        /// <summary>
        /// The worker will never actually complete but closes with the program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void BackgroundWorker1RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //  do nothing
        }

        /// <summary>
        /// This is the callback in the foreground that is triggered by the backgroundworker every second
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorker1ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            LoopProgramLoop();
        }


        /*-----------------------------------------------------------------------*/
        //  Form Style
        /*-----------------------------------------------------------------------*/

        /// <summary>
        /// Function that centers the text in the settings form (preview)
        /// </summary>
        private void ActionCentreTextSettingsForm()
        {
            int boxWidthCenter = groupBox_preview.Width / 2;
            int boxHeightCenter = groupBox_preview.Height / 2;

            int txtWidthCenter = label_preview.Width / 2;
            int txtHeightCenter = label_preview.Height / 2;

            int top = boxHeightCenter - txtHeightCenter;
            int left = boxWidthCenter - txtWidthCenter;

            label_preview.Location = new Point(left, top);
        }

        /// <summary>
        /// Function that centers the text in the output form (program)
        /// </summary>
        private void ActionCentreTextOutputForm()
        {
            if (_formOutput == null)
            {
                return;
            }

            if (!_formOutput.Visible)
            {
                return;
            }
            int boxWidthCenter = _formOutput.Width / 2;
            int boxHeightCenter = _formOutput.Height / 2;

            int txtWidthCenter = _formOutput.Controls["label_program"].Width / 2;
            int txtHeightCenter = _formOutput.Controls["label_program"].Height / 2;

            int top = boxHeightCenter - txtHeightCenter;
            int left = boxWidthCenter - txtWidthCenter;

            _formOutput.Controls["label_program"].Location = new Point(left, top);
        }

        /// <summary>
        /// Action that takes the font size from the input and sets it into the output form
        /// </summary>
        private void ActionTakeFontSize()
        {
            _fontSize = (int)numericUpDown_fontSize.Value;
            FontFamily fam = label_preview.Font.FontFamily;
            label_preview.Font = new Font(fam, _fontSize);
            _fontSize = (int)Math.Ceiling(_factorPreviewProgram * 1.2 * _fontSize);
            if (_formOutput == null) return;
            if (_formOutput.Visible)
                _formOutput.Controls["label_program"].Font = new Font(fam, _fontSize);
        }

        /// <summary>
        /// Function that will close the output form
        /// </summary>
        private void ActionCloseOutputForm()
        {
            _formOutput?.Hide();
            button_openOutputView.Text = "Open Program View";
        }

        /// <summary>
        /// Sets the color of the output form. Mainly for countdown flickering
        /// </summary>
        /// <param name="color">The color to which the output form text should be set</param>
        private void ActionSetOutputColor(Color color)
        {
            label_preview.ForeColor = color;
            if (_formOutput == null)
            {
                return;
            }

            if (_formOutput.Visible)
            {
                _formOutput.Controls["label_program"].ForeColor = color;
            }
        }

        /// <summary>
        /// Sets the configured time to the output view
        /// _programHours must be set for the hours
        /// _programMinutes must be set for the minutes
        /// _programSeconds could be set for the seconds
        /// Centers the text
        /// </summary>
        /// <param name="showSeconds">Enables or disables the seconds in the output form</param>
        private void ActionTimeToOutput(bool showSeconds)
        {
            ActionTakeFontSize();
            string outputText = "";

            if (_modeOutput != Mode.Idle)
            {
                outputText += _programHours.ToString("00");
                outputText += ":";
                outputText += _programMinutes.ToString("00");
                if (showSeconds)
                {
                    outputText += ":";
                    outputText += _programSeconds.ToString("00");
                }
            }

            label_preview.Text = outputText;
            if (_formOutput != null)
                if (_formOutput.Visible)
                    _formOutput.Controls["label_program"].Text = outputText;
            ActionCentreTextSettingsForm();
            ActionCentreTextOutputForm();
        }

        /*-----------------------------------------------------------------------*/
        //  Button actions
        /*-----------------------------------------------------------------------*/

        /// <summary>
        /// When the start button is pressed, this function calls the start countdown function.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonStartCountDownClick(object sender, EventArgs e)
        {
            ActionStartCountdown();
        }

        /// <summary>
        /// When the set time button is pressed, this function will set the countdown time to program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonCountdownSetTimeClick(object sender, EventArgs e)
        {
            if (_modeOutput != Mode.Countdown)
            {
                return;
            }
            
            if (radioButton_countdownTime.Checked) ActionSetCountdownTimers();
            ActionTakeCountdownTime();
            if (radioButton_countdownTime.Checked) _countdownStarted = true;
        }

        /// <summary>
        /// When the open output view button is pressed, this function will open the output window
        /// Sets all styles, fonts and colors correctly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonOpenOutputViewClick(object sender, EventArgs e)
        {
            //  create new instance if not yet existing
            if (_formOutput == null) _formOutput = new Form();

            if (_formOutput.Visible)
            {
                ActionCloseOutputForm();
            }
            else
            {
                //  set output form style
                _formOutput.FormBorderStyle = FormBorderStyle.None;
                _formOutput.Bounds = _screenSelectedOutputScreen.Bounds;
                _formOutput.KeyUp += ListenerDetectEscape;
                _formOutput.BackColor = Color.Black;

                _factorPreviewProgram = _formOutput.Bounds.Width / groupBox_preview.Width;

                //  add a label for the text to the program output if not yet present
                if (_formOutput.Controls.Count == 0)
                {
                    Label labelProgram = new Label
                    {
                        Text = "label_program placeholder",
                        Name = "label_program",
                        AutoSize = true
                    };
                    _formOutput.Controls.Add(labelProgram);
                }

                //  give color to the program label in the output
                _formOutput.Controls["label_program"].ForeColor = Color.White;

                //  centre the text in the output form
                ActionCentreTextOutputForm();

                //  add resize actions
                _formOutput.Resize += ListenerOutputFormResize;

                //  show the output form
                if (!_formOutput.Visible) _formOutput.Show();

                //  repeat bounds setting
                //  only seems to work after the .show()
                _formOutput.Bounds = _screenSelectedOutputScreen.Bounds;

                //  change open button text to close text
                button_openOutputView.Text = "Close Output View";
            }
        }

        /// <summary>
        /// Pause the countdown when the button is pressed. This is the handler for that
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonPauseCountdownClick(object sender, EventArgs e)
        {
            ActionPauseCountdown();
        }

        /// <summary>
        /// Stop the current countdown and enter the taken countdown time
        /// Countdown time must be taken (prepared) from the input first using ActionSetCountdownTimers
        /// </summary>
        private void ActionTakeCountdownTime()
        {
            _programHours = _countdownHours;
            _programMinutes = _countdownMinutes;
            _programSeconds = _countdownSeconds;
            _countdownOvertime = false;
            ActionPauseCountdown();
            ActionTimeToOutput(_secondsCountdown);
        }

        /// <summary>
        /// Pause the countdown
        /// </summary>
        private void ActionPauseCountdown()
        {
            _countdownStarted = false;
            if (_modeOutput != Mode.Countdown && !radioButton_countdownTime.Checked)
            {
                return;
            }

            button_pauseCountdown.Enabled = false;
            button_startCountDown.Enabled = true;
            button_startCountDown.BackColor = Color.FromArgb(255, 0, 192, 0);
            button_pauseCountdown.BackColor = Color.Silver;
        }

        /// <summary>
        /// Start / resume the countdown
        /// </summary>
        private void ActionStartCountdown()
        {
            if (_modeOutput != Mode.Countdown) return;
            
            _countdownStarted = true;
            switch (radioButton_countdownTime.Checked)
            {
                case false:
                    button_pauseCountdown.Enabled = true;
                    button_startCountDown.Enabled = false;
                    button_pauseCountdown.BackColor = Color.FromArgb(255, 255, 128, 0);
                    button_startCountDown.BackColor = Color.Silver;
                    break;
                case true:
                    ActionSetCountdownTimers();
                    break;
            }
        }

        /// <summary>
        /// Takes the countdown values from the input and loads them into the system to prepare
        /// Centres the text in preview
        /// Determines the right values depending on countdown mode
        /// </summary>
        private void ActionSetCountdownTimers()
        {
            if (_modeOutput != Mode.Countdown) return;
            
            if (radioButton_countdownDuration.Checked)
            {
                //  countdown for a given time period
                _countdownHours = (int)numericUpDown_hours.Value;
                _countdownMinutes = (int)numericUpDown_minutes.Value;
                _countdownSeconds = (int)numericUpDown_seconds.Value;
            }
            else
            {
                DateTime datetimeTo = dateTimePicker_countdownTime.Value;
                DateTime datetimeNow = DateTime.Now;
                TimeSpan timespanDiff = datetimeTo - datetimeNow;
                if (timespanDiff.Hours < 0 || timespanDiff.Minutes < 0 || timespanDiff.Seconds < 0)
                    _countdownOvertime = true;
                //  countdown to a given time
                _countdownHours = Math.Abs(timespanDiff.Hours);
                _countdownMinutes = Math.Abs(timespanDiff.Minutes);
                _countdownSeconds = Math.Abs(timespanDiff.Seconds);
            }

            ActionCentreTextSettingsForm();
            
        }

        /*-----------------------------------------------------------------------*/
        //  Other actions
        /*-----------------------------------------------------------------------*/

        /// <summary>
        /// Switches between the correct countdown mode based on the input given in the settings form
        /// </summary>
        private void ActionSetCountdownMode()
        {
            if (radioButton_countdownTime.Checked)
            {
                ActionSetCountdownTimers();
                ActionTakeCountdownTime();
                ActionStartCountdown();
                button_startCountDown.Enabled = false;
                button_pauseCountdown.Enabled = false;
                button_startCountDown.BackColor = Color.Silver;
                button_pauseCountdown.BackColor = Color.Silver;
            }
            else
            {
                ActionPauseCountdown();
                ActionSetCountdownTimers();
                ActionTakeCountdownTime();
            }
        }

        /// <summary>
        /// The loop that triggers every second to update the output and preview displayed time
        /// </summary>
        private void LoopProgramLoop()
        {
            switch (_modeOutput)
            {
                case Mode.Black:
                default:
                case Mode.Idle:
                    label_preview.Text = "";
                    ActionTimeToOutput(_secondsClock);
                    ActionCentreTextSettingsForm();
                    break;
                case Mode.Clock:
                    DateTime dateTime = DateTime.Now;
                    _programHours = short.Parse(dateTime.ToString("HH"));
                    _programMinutes = short.Parse(dateTime.ToString("mm"));
                    _programSeconds = short.Parse(dateTime.ToString("ss"));
                    ActionSetOutputColor(Color.White);
                    ActionTimeToOutput(_secondsClock);
                    break;
                case Mode.Countdown:

                    if (
                        (checkBox_countdownFlicker.Checked &&
                         _programSeconds <= numericUpDown_countdownFlicker.Value &&
                         _programHours == 0 &&
                         _programMinutes == 0) || _countdownOvertime
                    )
                    {
                        Color outputColor = _programSeconds % 2 == 0 ? Color.White : Color.Red;
                        ActionSetOutputColor(outputColor);
                    }
                    else
                    {
                        ActionSetOutputColor(Color.White);
                    }

                    ActionTimeToOutput(_secondsCountdown);

                    if (_countdownStarted)
                    {
                        if (_countdownOvertime && checkBox_overtime.Checked)
                        {
                            _programSeconds++;
                            if (_programSeconds > 59)
                            {
                                _programSeconds = 0;
                                _programMinutes++;
                            }

                            if (_programMinutes > 59)
                            {
                                _programMinutes = 0;
                                _programHours++;
                            }
                        }
                        else
                        {
                            _programSeconds--;
                            if (_programSeconds < 0)
                            {
                                _programSeconds = 59;
                                _programMinutes--;
                            }

                            if (_programMinutes < 0)
                            {
                                _programMinutes = 59;
                                _programHours--;
                            }

                            if (_programHours < 0)
                            {
                                _programHours = 0;
                                _programSeconds = 0;
                                _programMinutes = 0;
                                _countdownOvertime = true;
                                if (checkBox_overtime.Checked) _programSeconds = 1;
                            }
                        }
                    }

                    break;
            }
        }

        private enum Mode
        {
            Clock,
            Countdown,
            Black,
            Idle
        }
    }
}