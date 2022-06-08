using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace Clock_and_countdown
{
    public partial class Form1 : Form
    {
        private Form form_output;
        Screen[] screens = Screen.AllScreens;
        private Screen screen_selectedOutputScreen;

        private int fontSize = 35;

        private mode mode_output = mode.clock;

        private bool seconds_clock = false, seconds_countdown = true;

        private int countdown_hours = 0, countdown_minutes = 0, countdown_seconds = 0;
        private int program_hours, program_minutes, program_seconds;
        private int backgroundWorkCounter = 0;

        private bool countdown_started = false;
        private bool countdown_overtime = false;

        private float factor_previewProgram = 1;

        private enum mode
        {
            clock,
            countdown,
            black,
            idle
        }

        private System.ComponentModel.BackgroundWorker backgroundWorker1;

        public Form1()
        {
            InitializeComponent();

            //  Set the version from settings to the box in the right bottom corner
            //  set into label_version
            this.label_version.Text = Application.ProductVersion;

            //  center texts
            this.action_centreTextSettingsForm();

            
            //  detect screens
            this.action_detectScreens();

            //  add resize actions
            this.Resize += this.listener_settingsFormResize;

            //  close output screen on escape button
            this.KeyUp += listener_detectEscape;

            //  action to font size change event
            this.numericUpDown_fontSize.ValueChanged += listener_changeFontSize;

            
            //  detect change mode
            this.radioButton_mode_clock.CheckedChanged += listener_changeMode;
            this.radioButton_mode_countdown.CheckedChanged += listener_changeMode;
            this.radioButton_mode_idle_black.CheckedChanged += listener_changeMode;

            //  detect seconds view change
            this.checkBox_clock_seconds.CheckedChanged += listener_changeSecondsClock;
            this.checkBox_countdown_seconds.CheckedChanged += listener_changeSecondsCountdown;


            //  detect change of countdown
            this.numericUpDown_hours.ValueChanged += listener_changeDuration;
            this.numericUpDown_minutes.ValueChanged += listener_changeDuration;
            this.numericUpDown_seconds.ValueChanged += listener_changeDuration;
            this.dateTimePicker_countdownTime.ValueChanged += listener_changeDuration;

            //  set countdown timer format
            this.dateTimePicker_countdownTime.Format = DateTimePickerFormat.Time;
            this.dateTimePicker_countdownTime.ShowUpDown = true;

            this.radioButton_countdownTime.CheckedChanged += listener_changeCountdownMode;

            //  backgroundworker 
            //  foor loop every second
            InitializeBackgroundWorker();
        }

        /*-----------------------------------------------------------------------*/
        //  input changes & listeners
        /*-----------------------------------------------------------------------*/

        private void listener_changeSecondsClock(object sender, EventArgs e)
        {
            this.seconds_clock = this.checkBox_clock_seconds.Checked;
        }

        private void listener_changeSecondsCountdown(object sender, EventArgs e)
        {
            this.seconds_countdown = this.checkBox_countdown_seconds.Checked;
            this.action_setCountdownTimers();
        }

        private void listener_changeDuration(object sender, EventArgs e)
        {
            this.action_setCountdownTimers();
        }

        private void listener_changeMode(object sender, EventArgs e)
        {
            if (this.radioButton_mode_idle_black.Checked)
            {
                this.mode_output = mode.idle;
                this.action_pauseCountdown();
            }
            else if (this.radioButton_mode_countdown.Checked)
            {
                this.mode_output = mode.countdown;
                this.action_setCountdownMode();
            }
            else
            {
                this.action_pauseCountdown();
                this.mode_output = mode.clock;
                this.action_setCountdownTimers();
            }
        }

        private void listener_changeCountdownMode(object sender, EventArgs e)
        {
            if (this.mode_output == mode.countdown)
            {
                this.action_setCountdownMode();
            }
           
        }

        private void listener_settingsFormResize(object sender, EventArgs e)
        {
            this.action_centreTextSettingsForm();
        }

        private void listener_detectEscape(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                this.action_closeOutputForm();
            }
        }

        private void listener_changeFontSize(object sender, EventArgs e)
        {
            this.action_takeFontSize();
            this.action_centreTextSettingsForm();
        }

        private void listener_outputFormResize(object sender, EventArgs e)
        {
            this.action_centreTextOutputForm();
        }

        /*-----------------------------------------------------------------------*/
        //  Screen settings
        /*-----------------------------------------------------------------------*/

        private void action_detectScreens()
        {
            this.screen_selectedOutputScreen = this.screens[this.screens.Length - 1];
            foreach (Screen screen in this.screens)
            {
                this.comboBox_outputMonitor.Items.Add(screen.DeviceName.ToString());
            }
            this.comboBox_outputMonitor.SelectedIndex = this.screens.Length - 1;
            this.comboBox_outputMonitor.SelectedIndexChanged += this.listener_selectedScreenChanged;
        }

        private void listener_selectedScreenChanged(object sender, EventArgs e)
        {
            foreach (Screen screen in this.screens)
            {
                if (screen.DeviceName.ToString() == this.comboBox_outputMonitor.SelectedItem.ToString())
                {
                    this.screen_selectedOutputScreen = screen;
                    return;
                }
            }

            this.screen_selectedOutputScreen = this.screens[this.screens.Length - 1];
        }
    
        /*-----------------------------------------------------------------------*/
        //  Backgroundworker
        /*-----------------------------------------------------------------------*/
        private void InitializeBackgroundWorker()
        {
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.backgroundWorker1.WorkerReportsProgress = true;
            this.backgroundWorker1.WorkerSupportsCancellation = true;
            this.backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
            this.backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
            this.backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);
            this.backgroundWorker1.RunWorkerAsync();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                backgroundWorker1.ReportProgress(0, backgroundWorkCounter.ToString());
                System.Threading.Thread.Sleep(1000);
                backgroundWorkCounter++;
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //  do nothing
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.loop_programLoop();
        }


        /*-----------------------------------------------------------------------*/
        //  Form Style
        /*-----------------------------------------------------------------------*/

        private void action_centreTextSettingsForm()
        {
            var boxWidthCenter = this.groupBox_preview.Width / 2;
            var boxHeightCenter = this.groupBox_preview.Height / 2;

            var txtWidthCenter = this.label_preview.Width / 2;
            var txtHeightCenter = this.label_preview.Height / 2;

            var top = boxHeightCenter - txtHeightCenter;
            var left = boxWidthCenter - txtWidthCenter;

            this.label_preview.Location = new Point(left, top);
        }

        private void action_centreTextOutputForm()
        {
            if(this.form_output != null)
            {
                if(this.form_output.Visible)
                {
                    var boxWidthCenter = this.form_output.Width / 2;
                    var boxHeightCenter = this.form_output.Height / 2;

                    var txtWidthCenter = this.form_output.Controls["label_program"].Width / 2;
                    var txtHeightCenter = this.form_output.Controls["label_program"].Height / 2;

                    var top = boxHeightCenter - txtHeightCenter;
                    var left = boxWidthCenter - txtWidthCenter;

                    this.form_output.Controls["label_program"].Location = new Point(left, top);
                }
            }
        }

        private void action_takeFontSize()
        {

            this.fontSize = (int)this.numericUpDown_fontSize.Value;
            FontFamily fam = this.label_preview.Font.FontFamily;
            label_preview.Font = new Font(fam, this.fontSize);
            this.fontSize = (int) Math.Ceiling(this.factor_previewProgram * 1.2 * this.fontSize);
            if(this.form_output != null)
            {
                if (this.form_output.Visible)
                {
                    this.form_output.Controls["label_program"].Font = new Font(fam, fontSize);
                }
            }
        }

        private void action_closeOutputForm()
        {
            this.form_output?.Hide();
            this.button_openOutputView.Text = "Open Program View";
        }

        private void action_setOutputColor(Color color)
        {
            this.label_preview.ForeColor = color;
            if (this.form_output != null)
            {
                if (this.form_output.Visible)
                {
                    this.form_output.Controls["label_program"].ForeColor = color;
                }
            }

        }

        private void action_timeToOutput(bool showSeconds)
        {
            this.action_takeFontSize();
            String outputText = "";

            outputText += program_hours.ToString("00");
            outputText += ":";
            outputText += program_minutes.ToString("00");
            if (showSeconds)
            {
                outputText += ":";
                outputText += program_seconds.ToString("00");
            }

            this.label_preview.Text = outputText;
            if (this.form_output != null)
            {
                if (this.form_output.Visible)
                {
                    this.form_output.Controls["label_program"].Text = outputText;
                }
            }
            this.action_centreTextSettingsForm();
            this.action_centreTextOutputForm();
        }

        /*-----------------------------------------------------------------------*/
        //  Button actions
        /*-----------------------------------------------------------------------*/

        private void button_startCountDown_Click(object sender, EventArgs e)
        {
            this.action_startCountdown();
        }

        private void button_countdownSetTime_Click(object sender, EventArgs e)
        {
            if(this.mode_output == mode.countdown)
            {
                if(this.radioButton_countdownTime.Checked)
                {
                    this.action_setCountdownTimers();
                }
                this.action_takeCountdownTime();
                if (this.radioButton_countdownTime.Checked)
                {
                    this.countdown_started = true;
                    
                }
            }
        }

        private void button_openOutputView_Click(object sender, EventArgs e)
        {

            //  create new instance if not yet existing
            if (this.form_output == null)
            {
                this.form_output = new Form();
            }
            
            if (this.form_output.Visible)
            {
                action_closeOutputForm();
            }
            else
            {
                //  set output form style
                this.form_output.FormBorderStyle = FormBorderStyle.None;
                this.form_output.Bounds = this.screen_selectedOutputScreen.Bounds;
                this.form_output.KeyUp += this.listener_detectEscape;
                this.form_output.BackColor = Color.Black;

                this.factor_previewProgram = form_output.Bounds.Width / this.groupBox_preview.Width;

                //  add a label for the text to the program output if not yet present
                if (this.form_output.Controls.Count == 0)
                {
                    Label label_program = new Label();
                    label_program.Text = "label_program placeholder";
                    label_program.Name = "label_program";
                    label_program.AutoSize = true;
                    this.form_output.Controls.Add(label_program);
                }

                //  give color to the program label in the output
                this.form_output.Controls["label_program"].ForeColor = Color.White;

                //  show the output form
                if(!this.form_output.Visible)
                {
                    this.form_output.Show();
                }

                //  centre the text in the output form
                this.action_centreTextOutputForm();

                //  add resize actions
                form_output.Resize += this.listener_outputFormResize;

                //  change open button text to close text
                this.button_openOutputView.Text = "Close Output View";
            }
        }

        private void buttonPauseCountdown_Click(object sender, EventArgs e)
        {
            this.action_pauseCountdown();
        }

        private void action_takeCountdownTime()
        {
            this.program_hours = this.countdown_hours;
            this.program_minutes = this.countdown_minutes;
            this.program_seconds = this.countdown_seconds;
            this.countdown_overtime = false;
            this.action_pauseCountdown();
            this.action_timeToOutput(this.seconds_countdown);
        }

        private void action_pauseCountdown()
        {
            this.countdown_started = false;
            if (this.mode_output == mode.countdown && !this.radioButton_countdownTime.Checked)
            {
                this.button_pauseCountdown.Enabled = false;
                this.button_startCountDown.Enabled = true;
                this.button_startCountDown.BackColor = Color.FromArgb(255, 0, 192, 0);
                this.button_pauseCountdown.BackColor = Color.Silver;
            }

        }

        private void action_startCountdown()
        {
            if (this.mode_output == mode.countdown)
            {
                this.countdown_started = true;
                if (!this.radioButton_countdownTime.Checked)
                {
                    this.button_pauseCountdown.Enabled = true;
                    this.button_startCountDown.Enabled = false;
                    this.button_pauseCountdown.BackColor = Color.FromArgb(255, 255, 128, 0);
                    this.button_startCountDown.BackColor = Color.Silver;
                }

                if(this.radioButton_countdownTime.Checked)
                {
                    this.action_setCountdownTimers();
                }
            }


        }

        private void action_setCountdownTimers()
        {
            if (this.mode_output == mode.countdown)
            {
                if (this.radioButton_countdownDuration.Checked)
                {
                    //  countdown for a given time period
                    this.countdown_hours = (int)this.numericUpDown_hours.Value;
                    this.countdown_minutes = (int)this.numericUpDown_minutes.Value;
                    this.countdown_seconds = (int)this.numericUpDown_seconds.Value;
                }
                else
                {
                    DateTime datetime_to = this.dateTimePicker_countdownTime.Value;
                    DateTime datetime_now = DateTime.Now;
                    TimeSpan timespan_diff = datetime_to - datetime_now;
                    if(timespan_diff.Hours < 0 || timespan_diff.Minutes < 0 || timespan_diff.Seconds < 0)
                    {
                        countdown_overtime = true;
                    }
                    //  countdown to a given time
                    this.countdown_hours = Math.Abs(timespan_diff.Hours);
                    this.countdown_minutes = Math.Abs(timespan_diff.Minutes);
                    this.countdown_seconds = Math.Abs(timespan_diff.Seconds);
                }
                this.action_centreTextSettingsForm();
            }
        }

        /*-----------------------------------------------------------------------*/
        //  Other actions
        /*-----------------------------------------------------------------------*/

        private void action_setCountdownMode()
        {
            if (this.radioButton_countdownTime.Checked)
            {

                this.action_setCountdownTimers();
                this.action_takeCountdownTime();
                this.action_startCountdown();
                this.button_startCountDown.Enabled = false;
                this.button_pauseCountdown.Enabled = false;
                this.button_startCountDown.BackColor = Color.Silver;
                this.button_pauseCountdown.BackColor = Color.Silver;
                
                
            }
            else
            {
                this.action_pauseCountdown();
                this.action_setCountdownTimers();
                this.action_takeCountdownTime();
            }
        }

        private void loop_programLoop()
        {
            switch(this.mode_output)
            {
                case mode.black:
                default:
                case mode.idle:
                    this.label_preview.Text = "";
                    this.action_centreTextSettingsForm();
                    break;
                case mode.clock:
                    DateTime dateTime = DateTime.Now;
                    this.program_hours = Int16.Parse(dateTime.ToString("HH"));
                    this.program_minutes = Int16.Parse(dateTime.ToString("mm"));
                    this.program_seconds = Int16.Parse(dateTime.ToString("ss"));
                    this.action_setOutputColor(Color.White);
                    this.action_timeToOutput(this.seconds_clock);
                    break;
                case mode.countdown:
                    
                    if(
                        this.checkBox_countdownFlicker.Checked && 
                        (
                            this.program_seconds <= numericUpDown_countdownFlicker.Value &&
                            this.program_hours == 0 && 
                            this.program_minutes == 0
                        ) || (
                            this.countdown_overtime
                        )
                    )
                    {
                        Color outputColor = this.program_seconds % 2 == 0 ? Color.White : Color.Red;
                        this.action_setOutputColor(outputColor);
                       
                    }
                    else
                    {
                        this.action_setOutputColor(Color.White);
                    }

                    this.action_timeToOutput(this.seconds_countdown);

                    if (this.countdown_started)
                    {
                        if(this.countdown_overtime && this.checkBox_overtime.Checked)
                        {
                            this.program_seconds++;
                            if (this.program_seconds > 59)
                            {
                                this.program_seconds = 0;
                                this.program_minutes++;
                            }

                            if (this.program_minutes > 59)
                            {
                                this.program_minutes = 0;
                                this.program_hours++;
                            }
                        }
                        else
                        {
                            this.program_seconds--;
                            if (this.program_seconds < 0)
                            {
                                this.program_seconds = 59;
                                this.program_minutes--;
                            }

                            if (this.program_minutes < 0)
                            {
                                this.program_minutes = 59;
                                this.program_hours--;
                            }

                            if (this.program_hours < 0)
                            {
                                this.program_hours = 0;
                                this.program_seconds = 0;
                                this.program_minutes = 0;
                                this.countdown_overtime = true;
                                if(this.checkBox_overtime.Checked)
                                {
                                    this.program_seconds = 1;
                                }
                            }
                        }
                        
                    }
                    break;
            }
        }
    }
}
