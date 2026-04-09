namespace ladder;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

public partial class Form1 : Form
{
    const int COLS = 8, ROWS = 10;
    const int MARGIN = 60, TOP = 90, BOTTOM_Y = 410;
    int ColX(int c) => MARGIN + c * ((ClientSize.Width - MARGIN * 2) / (COLS - 1));
    int RowY(int r) => TOP + r * ((BOTTOM_Y - TOP) / ROWS);

    // --- State ---
    List<(int row, int col)> rungs = new();
    int animCol, animRow;
    int selectedStart = 0;
    bool running = false;
    Timer timer = new Timer { Interval = 80 };
    readonly List<(int col, int row)> trail = [];  // waypoints of the current/last path

    readonly string[] playerNames = ["P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8"];
    readonly string[] results = new string[COLS];  // results[finalCol] = playerName who landed there
    readonly bool[] played = new bool[COLS];
    readonly Button[] playerButtons = new Button[COLS];


    public Form1()
    {
        Text = "阿弥陀籤"; Width = 700; Height = 780;
        DoubleBuffered = true;
        GenerateRungs();
        timer.Tick += (s, e) => Step();
        Load += (s, e) => CreatePlayerButtons();
    }

    void CreatePlayerButtons()
    {
        for (int c = 0; c < COLS; c++)
        {
            int col = c; // capture for closure
            var btn = new Button
            {
                Text = playerNames[col],
                Width = 52,
                Height = 26,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(Font.FontFamily, 9f, FontStyle.Bold),
                BackColor = Color.LightSteelBlue,
                FlatStyle = FlatStyle.Flat,
            };
            btn.Click += (s, e) => StartPlayer(col);
            Controls.Add(btn);
            playerButtons[col] = btn;
        }
        PositionButtons();
    }

    void PositionButtons()
    {
        for (int c = 0; c < COLS; c++)
            playerButtons[c].Location = new Point(ColX(c) - 26, TOP - 58);
    }

    void StartPlayer(int col)
    {
        if (running || played[col]) return;
        selectedStart = col;
        animCol = col;
        animRow = 0;
        trail.Clear();
        trail.Add((col, 0));
        running = true;
        timer.Start();
        Invalidate();
    }

    void GenerateRungs()
    {
        rungs.Clear();
        var rand = new Random();
        for (int r = 1; r < ROWS; r++)
        {
            var usedCols = new HashSet<int>();
            for (int c = 0; c < COLS - 1; c++)
            {
                if (!usedCols.Contains(c) && !usedCols.Contains(c + 1) && rand.Next(2) == 0)
                {
                    rungs.Add((r, c));
                    usedCols.Add(c); usedCols.Add(c + 1);
                }
            }
        }
    }

    void Step()
    {
        int prevCol = animCol;
        animRow++;
        // always record the vertical step first (straight down)
        trail.Add((prevCol, animRow));
        foreach (var (row, col) in rungs)
        {
            if (row == animRow)
            {
                if (col == animCol)      { animCol++; break; }
                if (col + 1 == animCol)  { animCol--; break; }
            }
        }
        // if a rung was taken, record the horizontal slide
        if (animCol != prevCol)
            trail.Add((animCol, animRow));
        if (animRow >= ROWS)
        {
            timer.Stop();
            running = false;
            played[selectedStart] = true;
            results[animCol] = playerNames[selectedStart];
            playerButtons[selectedStart].Enabled = false;
        }
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.White);

        var linePen = new Pen(Color.Black, 2);
        var rungPen = new Pen(Color.DarkBlue, 3);

        // Draw vertical lines
        for (int c = 0; c < COLS; c++)
            g.DrawLine(linePen, ColX(c), TOP, ColX(c), BOTTOM_Y);

        // Draw rungs
        foreach (var (row, col) in rungs)
            g.DrawLine(rungPen, ColX(col), RowY(row), ColX(col + 1), RowY(row));

        // Bottom: position number + player result underneath
        for (int c = 0; c < COLS; c++)
        {
            g.DrawString($"{c + 1}", Font, Brushes.Black, ColX(c) - 5, BOTTOM_Y + 8);
            if (!string.IsNullOrEmpty(results[c]))
            {
                var resultFont = new Font(Font.FontFamily, 8.5f, FontStyle.Bold);
                g.DrawString(results[c], resultFont, Brushes.DarkGreen, ColX(c) - 14, BOTTOM_Y + 30);
            }
        }

        // Snake trail
        if (trail.Count > 1)
        {
            var pts = trail.Select(p => new Point(ColX(p.col), RowY(p.row))).ToArray();
            using var trailPen = new Pen(Color.Red, 4) {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap   = System.Drawing.Drawing2D.LineCap.Round,
                LineJoin = System.Drawing.Drawing2D.LineJoin.Round,
            };
            g.DrawLines(trailPen, pts);
            // filled circle at the head
            var head = pts[^1];
            g.FillEllipse(Brushes.Red, head.X - 7, head.Y - 7, 14, 14);
            if (running)
                g.DrawString(playerNames[selectedStart], Font, Brushes.Red, head.X + 10, head.Y - 8);
        }

        // Status text at bottom
        bool allPlayed = Array.TrueForAll(played, p => p);
        bool noneStarted = !Array.Exists(played, p => p);

        if (!running && noneStarted && animRow == 0)
            g.DrawString("Click a player button to start!", Font, Brushes.Gray, 100, 480);
        else if (!running && animRow >= ROWS && !allPlayed)
            g.DrawString($"{playerNames[selectedStart]} → #{animCol + 1}   (Next player's turn)",
                Font, Brushes.ForestGreen, 60, 480);
        else if (allPlayed)
            g.DrawString("All done!  Click anywhere to restart.", Font, Brushes.Purple, 70, 480);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        bool allPlayed = Array.TrueForAll(played, p => p);
        if (!running && allPlayed)
        {
            GenerateRungs();
            animRow = 0;
            trail.Clear();
            Array.Clear(results, 0, results.Length);
            Array.Clear(played, 0, played.Length);
            for (int c = 0; c < COLS; c++)
                playerButtons[c].Enabled = true;
            Invalidate();
        }
    }
}
