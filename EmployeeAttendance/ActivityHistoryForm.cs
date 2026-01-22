using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EmployeeAttendance
{
    public class ActivityHistoryForm : Form
    {
        private string username = "";
        private string displayName = "";
        private DateTime currentMonth = DateTime.Now;
        private Dictionary<int, AttendanceStatus> monthlyData = new Dictionary<int, AttendanceStatus>();
        
        // Colors
        private readonly Color bgColor = Color.FromArgb(26, 26, 26);
        private readonly Color cardColor = Color.FromArgb(37, 37, 37);
        private readonly Color textColor = Color.FromArgb(224, 224, 224);
        private readonly Color mutedColor = Color.FromArgb(138, 138, 138);
        private readonly Color borderColor = Color.FromArgb(58, 58, 58);
        
        // Status colors
        private readonly Color presentColor = Color.FromArgb(34, 197, 94);     // Green
        private readonly Color absentColor = Color.FromArgb(239, 68, 68);     // Red
        private readonly Color halfDayColor = Color.FromArgb(251, 146, 60);   // Orange
        private readonly Color paidLeaveColor = Color.FromArgb(168, 85, 247); // Purple
        private readonly Color weekOffColor = Color.FromArgb(100, 116, 139);  // Gray
        
        private Label employeeNameLabel = null!;
        private Label monthYearLabel = null!;
        private Panel statisticsPanel = null!;
        private Panel calendarPanel = null!;
        private Panel detailsPanel = null!;
        
        private enum AttendanceStatus { Present, Absent, HalfDay, PaidLeave, WeekOff, NoData }
        
        public ActivityHistoryForm(string user, string displayNameInput)
        {
            username = user;
            displayName = displayNameInput;
            InitializeComponent();
            LoadMonthlyData(currentMonth);
        }
        
        private void InitializeComponent()
        {
            this.Text = "Attendance History";
            this.Size = new Size(650, 1050);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = bgColor;
            this.Font = new Font("Segoe UI", 10F);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                BackColor = bgColor,
                AutoScroll = true
            };
            
            int y = 10;
            
            // Employee name with back button
            var headerPanel = new Panel
            {
                Location = new Point(10, y),
                Size = new Size(600, 40),
                BackColor = Color.Transparent
            };
            
            var backButton = new Button
            {
                Text = "← ",
                Location = new Point(0, 5),
                Size = new Size(30, 30),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.Transparent,
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand
            };
            backButton.Click += (s, e) => this.Close();
            headerPanel.Controls.Add(backButton);
            
            employeeNameLabel = new Label
            {
                Text = displayName,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = textColor,
                Location = new Point(40, 8),
                AutoSize = true
            };
            headerPanel.Controls.Add(employeeNameLabel);
            mainPanel.Controls.Add(headerPanel);
            y += 50;
            
            // Month Year Selector
            var monthControlPanel = new Panel
            {
                Location = new Point(10, y),
                Size = new Size(600, 45),
                BackColor = Color.Transparent
            };
            
            var prevButton = new Button
            {
                Text = "◄",
                Location = new Point(0, 10),
                Size = new Size(40, 30),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = borderColor,
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            prevButton.Click += (s, e) =>
            {
                currentMonth = currentMonth.AddMonths(-1);
                LoadMonthlyData(currentMonth);
            };
            monthControlPanel.Controls.Add(prevButton);
            
            monthYearLabel = new Label
            {
                Text = currentMonth.ToString("MMMM yyyy"),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = textColor,
                Location = new Point(220, 10),
                AutoSize = true
            };
            monthControlPanel.Controls.Add(monthYearLabel);
            
            var nextButton = new Button
            {
                Text = "►",
                Location = new Point(560, 10),
                Size = new Size(40, 30),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = borderColor,
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            nextButton.Click += (s, e) =>
            {
                currentMonth = currentMonth.AddMonths(1);
                LoadMonthlyData(currentMonth);
            };
            monthControlPanel.Controls.Add(nextButton);
            mainPanel.Controls.Add(monthControlPanel);
            y += 55;
            
            // Statistics Panel
            statisticsPanel = new Panel
            {
                Location = new Point(10, y),
                Size = new Size(600, 80),
                BackColor = cardColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            mainPanel.Controls.Add(statisticsPanel);
            y += 95;
            
            // Calendar Panel - Larger to fit all dates
            calendarPanel = new Panel
            {
                Location = new Point(10, y),
                Size = new Size(600, 350),
                BackColor = cardColor,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = false
            };
            mainPanel.Controls.Add(calendarPanel);
            y += 365;
            
            // Details Panel
            detailsPanel = new Panel
            {
                Location = new Point(10, y),
                Size = new Size(600, 250),
                BackColor = cardColor,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true,
                Padding = new Padding(15)
            };
            mainPanel.Controls.Add(detailsPanel);
            
            this.Controls.Add(mainPanel);
            this.ResumeLayout(false);
        }
        
        private void LoadMonthlyData(DateTime month)
        {
            monthYearLabel.Text = month.ToString("MMMM yyyy");
            monthlyData.Clear();
            
            // Get attendance data for the month from database
            // For each day in the month, query the punch_log_consolidated table
            int daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
            
            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(month.Year, month.Month, day);
                var activity = DatabaseHelper.GetActivityForDate(username, date);
                
                // Determine status based on punch in/out
                AttendanceStatus status = AttendanceStatus.NoData;
                
                if (activity.HasActivity)
                {
                    // Has punch in record
                    if (activity.PunchOutTime.HasValue)
                    {
                        // Has both punch in and punch out = Present (worked that day)
                        status = AttendanceStatus.Present;
                    }
                    else
                    {
                        // Has punch in but no punch out = Half day or incomplete
                        status = AttendanceStatus.HalfDay;
                    }
                }
                else
                {
                    // No punch in at all = Absent
                    status = AttendanceStatus.Absent;
                }
                
                monthlyData[day] = status;
            }
            
            UpdateStatistics();
            DrawCalendar();
            ClearDetailsPanel();
        }
        
        private void UpdateStatistics()
        {
            statisticsPanel.Controls.Clear();
            
            int present = monthlyData.Values.Count(x => x == AttendanceStatus.Present);
            int absent = monthlyData.Values.Count(x => x == AttendanceStatus.Absent);
            int halfDay = monthlyData.Values.Count(x => x == AttendanceStatus.HalfDay);
            int paidLeave = monthlyData.Values.Count(x => x == AttendanceStatus.PaidLeave);
            int weekOff = monthlyData.Values.Count(x => x == AttendanceStatus.WeekOff);
            
            int statWidth = 100;
            int statHeight = 70;
            int spacing = 10;
            int startX = 10;
            int y = 5;
            
            CreateStatBox(statisticsPanel, "Present", present.ToString(), presentColor, startX, y, statWidth, statHeight);
            startX += statWidth + spacing;
            
            CreateStatBox(statisticsPanel, "Absent", absent.ToString(), absentColor, startX, y, statWidth, statHeight);
            startX += statWidth + spacing;
            
            CreateStatBox(statisticsPanel, "Half Day", halfDay.ToString(), halfDayColor, startX, y, statWidth, statHeight);
            startX += statWidth + spacing;
            
            CreateStatBox(statisticsPanel, "Paid Leave", paidLeave.ToString(), paidLeaveColor, startX, y, statWidth, statHeight);
        }
        
        private void CreateStatBox(Panel parent, string label, string value, Color bgColor, int x, int y, int width, int height)
        {
            var box = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = bgColor,
                BorderStyle = BorderStyle.None
            };
            
            var labelControl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(5, 5),
                AutoSize = true
            };
            box.Controls.Add(labelControl);
            
            var valueControl = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(5, 25),
                AutoSize = true
            };
            box.Controls.Add(valueControl);
            
            parent.Controls.Add(box);
        }
        
        private void DrawCalendar()
        {
            calendarPanel.Controls.Clear();
            
            // Days header
            string[] dayNames = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            int cellWidth = 70;
            int cellHeight = 50;
            int y = 10;
            
            for (int i = 0; i < 7; i++)
            {
                var dayLabel = new Label
                {
                    Text = dayNames[i],
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = mutedColor,
                    Location = new Point(10 + i * cellWidth, y),
                    Size = new Size(cellWidth - 5, 25),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                calendarPanel.Controls.Add(dayLabel);
            }
            y += 35;
            
            // Get first day of month
            var firstDay = new DateTime(currentMonth.Year, currentMonth.Month, 1);
            int startDayOfWeek = (int)firstDay.DayOfWeek;
            int daysInMonth = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);
            
            int day = 1;
            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    if ((row == 0 && col < startDayOfWeek) || day > daysInMonth)
                        continue;
                    
                    int x = 10 + col * cellWidth;
                    var status = monthlyData.ContainsKey(day) ? monthlyData[day] : AttendanceStatus.NoData;
                    Color statusColor = GetStatusColor(status);
                    
                    var dayButton = new Button
                    {
                        Text = day.ToString(),
                        Location = new Point(x, y),
                        Size = new Size(cellWidth - 5, cellHeight),
                        Font = new Font("Segoe UI", 12, FontStyle.Bold),
                        BackColor = statusColor,
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        Cursor = Cursors.Hand,
                        Tag = day
                    };
                    dayButton.FlatAppearance.BorderSize = 0;
                    dayButton.Click += (s, e) =>
                    {
                        int clickedDay = (int)dayButton.Tag;
                        ShowDateDetails(clickedDay);
                    };
                    calendarPanel.Controls.Add(dayButton);
                    
                    day++;
                }
                y += cellHeight + 5;
            }
        }
        
        private Color GetStatusColor(AttendanceStatus status)
        {
            return status switch
            {
                AttendanceStatus.Present => presentColor,
                AttendanceStatus.Absent => absentColor,
                AttendanceStatus.HalfDay => halfDayColor,
                AttendanceStatus.PaidLeave => paidLeaveColor,
                AttendanceStatus.WeekOff => weekOffColor,
                _ => Color.FromArgb(80, 80, 80)  // Gray for no data
            };
        }
        
        private void ShowDateDetails(int day)
        {
            var date = new DateTime(currentMonth.Year, currentMonth.Month, day);
            var activity = DatabaseHelper.GetActivityForDate(username, date);
            
            detailsPanel.Controls.Clear();
            
            var dateHeader = new Label
            {
                Text = $"📅 {date:dddd, MMMM d, yyyy}",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = textColor,
                Location = new Point(15, 15),
                AutoSize = true
            };
            detailsPanel.Controls.Add(dateHeader);
            
            int detailY = 50;
            
            if (!activity.HasActivity)
            {
                var noActivityLabel = new Label
                {
                    Text = "No activity recorded for this date.",
                    Font = new Font("Segoe UI", 11),
                    ForeColor = mutedColor,
                    Location = new Point(15, detailY),
                    AutoSize = true
                };
                detailsPanel.Controls.Add(noActivityLabel);
                return;
            }
            
            // Add activity details using corrected calculations
            AddDetailRow(detailsPanel, "▶ Punch In", activity.FormattedPunchIn, Color.FromArgb(16, 185, 129), detailY);
            detailY += 35;
            
            AddDetailRow(detailsPanel, "⏹ Punch Out", activity.FormattedPunchOut, Color.FromArgb(239, 68, 68), detailY);
            detailY += 35;
            
            AddDetailRow(detailsPanel, "☕ Break Time", activity.FormattedBreakDuration, Color.FromArgb(245, 158, 11), detailY);
            detailY += 35;
            
            AddDetailRow(detailsPanel, "⏰ Total Work Hours", activity.FormattedNetWork, Color.FromArgb(37, 99, 235), detailY);
            detailY += 35;
            
            AddDetailRow(detailsPanel, "⛔ Total Inactivity", activity.FormattedInactivity, Color.FromArgb(156, 163, 175), detailY);
        }
        
        private void AddDetailRow(Panel parent, string label, string value, Color valueColor, int y)
        {
            var labelControl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = mutedColor,
                Location = new Point(15, y),
                AutoSize = true
            };
            parent.Controls.Add(labelControl);
            
            var valueControl = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = valueColor,
                Location = new Point(200, y),
                AutoSize = true
            };
            parent.Controls.Add(valueControl);
        }
        
        private void ClearDetailsPanel()
        {
            detailsPanel.Controls.Clear();
            
            var defaultLabel = new Label
            {
                Text = "Click a date to view details",
                Font = new Font("Segoe UI", 11),
                ForeColor = mutedColor,
                Location = new Point(15, 100),
                AutoSize = true
            };
            detailsPanel.Controls.Add(defaultLabel);
        }
    }
}
