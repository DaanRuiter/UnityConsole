using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System;

// Unity Console
// Version 1.0.1
// By: Daan Ruiter
// http://daanruiter.net
// contact@daanruiter.net

//MIT License

//Copyright (c) 2016
//Daan Ruiter

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

namespace DaanRuiter.Util
{
    /// <summary>
    /// Info container for console log entries
    /// </summary>
    public class ConsolePrintInfo
    {
        private LogType m_type;
        private string m_message;
        private string m_stackTrace;
        private bool m_command;
        private Color m_color;

        private int m_stackSize = 0;
        private int m_lines;

        /// <summary>
        /// Construcs the ConsolePrintInfo
        /// </summary>
        /// <param name="type">The type of log. Is either: Log, Warning, Error or Exception</param>
        /// <param name="message">The message to display in the console</param>
        /// <param name="stackTrace">The stacktrace to the where the log was called from</param>
        /// <param name="command">Is this a command (not used ATM)</param>
        /// <param name="color">Custom color of the message</param>
        public ConsolePrintInfo (LogType type = LogType.Log, string message = "", string stackTrace = "", bool command = false, Color color = default(Color))
        {
            m_type = type;
            m_message = message;
            m_stackTrace = stackTrace;
            m_command = command;
            m_color = color;

            m_lines = message.Split('\n').Length;
            if (m_lines == 1)
                m_lines = 0;

            if (m_stackTrace != "")
            {
                m_stackSize = stackTrace.Split('\n').Length;
            }
        }

        /// <summary>
        /// The type of log. Is either: Log, Warning, Error or Exception
        /// </summary>
        public LogType type
        {
            get
            {
                return m_type;
            }
        }
        /// <summary>
        /// The message to display in the console
        /// </summary>
        public string message
        {
            get
            {
                return m_message;
            }
        }
        /// <summary>
        /// The stacktrace to the where the log was called from
        /// </summary>
        public string stackTrace
        {
            get
            {
                return m_stackTrace;
            }
        }
        /// <summary>
        /// Was this a command (not used ATM)
        /// </summary>
        public bool command
        {
            get
            {
                return m_command;
            }
        }
        /// <summary>
        /// The size of the stacktrace
        /// </summary>
        public int stackSize
        {
            get
            {
                return m_stackSize;
            }
        }
        /// <summary>
        /// The color if a custom color was set
        /// </summary>
        public Color color
        {
            get
            {
                return m_color;
            }
        }
        /// <summary>
        /// The amount of lines in the message
        /// </summary>
        public int lineCount
        {
            get
            {
                return m_lines;
            }
        }

        /// <summary>
        /// Set the color of the log entry
        /// </summary>
        /// <param name="color"></param>
        public ConsolePrintInfo SetColor(Color color)
        {
            m_color = color;
            return this;
        }
    }
    /// <summary>
    /// Add this attribute on your commands and they will automaticly be added to the console's list
    /// </summary>
    public class ConsoleCommand : Attribute
    {
        private bool m_hidden;
        private string m_description;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="description">Description that is displayed When ListCommands is called</param>
        /// <param name="hidden">If the method should be displayed wehn ListCommadns is called</param>
        public ConsoleCommand (string description = "", bool hidden = false)
        {
            m_hidden = hidden;
            m_description = description;
        }

        /// <summary>
        /// Is the command hidden
        /// </summary>
        public bool hidden
        {
            get
            {
                return m_hidden;
            }
        }
        /// <summary>
        /// Description of the command, displayed when ListCommands is called
        /// </summary>
        public string description
        {
            get
            {
                return m_description;
            }
        }
    }

    /// <summary>
    /// The main class of the console, you only need to call this class to fully make use of the console
    /// To add your own commands, give a reference to your project's assembly as parameter when calling RefreshCommands()
    /// Example: CMD.RefreshCommands(new Assembly[] { Assembly.GetAssembly(typeof(ANY_MONOBEHAVIOUR_SCRIPT_IN_YOUR_PROJECT)) } );
    /// </summary>
    public class CMD : MonoBehaviour
    {
        //Instance
        private static CMD m_instance;

        //Switches
        private static bool m_forceDisable = false;
        private static bool m_enabled = false;
        private static bool m_isTyping = false;
        private static bool m_usingBackspace = false;
        private static bool m_isDragging = false;
        private static bool m_isResizing = false;

        //Input
        private static string m_inputString;
        private static List<string> m_previousCommands = new List<string>();
        private static int m_previousCommandIndex = -1;

        //Drag
        private static Vector3 m_dragStartMousePosition;
        private static Vector2 m_dragStartWindowPosition;
        private static Vector2 m_dragStartTitlePosition;
        private static Vector2 m_dragStartInputPosition;
        private static Vector2 m_dragStartClosePosition;

        //Resize
        private static Vector3 m_resizeStartMousePosition;
        private static Vector2 m_resizeStartWindowSize;

        private static Vector2 m_mouseDownPosition;
        //-----------------------------------------------------------------------//
        //CONFIG
        private static int m_fontSize = 12;
        private static float m_widthPercentage = 50f;
        private static float m_heightPercentage = 60f;
        private static KeyCode m_enableKeyCode = KeyCode.F1;

        private static Vector2 m_minimalSize = new Vector2(100, 50);

        private static Color m_textColor = new Color(0.85f, 0.85f, 0.85f);
        private static Color m_inputActiveBackgroundColor = new Color(0.45f, 0.45f, 0.45f, 0.9f);
        private static Color m_inputInactiveBackgroundColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);

        private static bool m_openConsoleOnError = true;
        //-----------------------------------------------------------------------//
        //GUIskin
        private static GUISkin m_skin;

        //backgrounds
        private static Texture2D m_windowBackgroundTexture;
        private static Texture2D m_inputFieldInactiveTexture;
        private static Texture2D m_inputFieldActiveTexture;
        private static Texture2D m_resizeAreaTexture;
        private static Texture2D m_titleBarTexture;
        private static Texture2D m_closeButtonTexture;

        //Cursors
        private static Texture2D m_resizeCursorTexture;
        private static Texture2D m_inputCursorTexture;
        private static Texture2D m_moveCursorTexture;
        private static Texture2D m_clickCursorTexture;

        //Rectengles
        private static Rect m_windowRect;
        private static Rect m_inputFieldRect;
        private static Rect m_titleRect;
        private static Rect m_resizeArea;
        private static Rect m_closeButtonRect;

        //Print commands
        private static List<ConsolePrintInfo> m_printCommands;
        private static List<MethodInfo> m_methods;

        #region Initial Methods
        /// <summary>
        /// Initializes the Console, must be called to be able to use it
        /// </summary>
        /// <param name="consoleObject">The gameobject the initialized console component is added to</param>
        public static void Init (GameObject consoleObject)
        {
            if (m_forceDisable)
                return;

            //Add to gameobject
            if (consoleObject.GetComponent<CMD>() == null)
                consoleObject.AddComponent<CMD>();

            //Intialize our GUI skin
            m_skin = ScriptableObject.CreateInstance<GUISkin>();
            m_skin.GetStyle("Label").normal.textColor = m_textColor;
            m_skin.GetStyle("Label").alignment = TextAnchor.LowerLeft;
            m_skin.GetStyle("Label").wordWrap = true;
            m_skin.GetStyle("Label").fontSize = m_fontSize;

            //Get console window size
            float width = Screen.width / 100f * m_widthPercentage;
            float height = Screen.height / 100f * m_heightPercentage;
            float inputHeight = m_fontSize + 2;

            //Create rectangles
            m_windowRect = new Rect(0, Screen.height - height - inputHeight, width, height);
            m_titleRect = new Rect(0, Screen.height - height - inputHeight * 2, width - inputHeight, inputHeight);
            m_inputFieldRect = new Rect(0, Screen.height - inputHeight, width - inputHeight, inputHeight);
            m_resizeArea = new Rect(width - inputHeight, Screen.height - inputHeight, inputHeight, inputHeight);
            m_closeButtonRect = new Rect(width - inputHeight, Screen.height - height - inputHeight * 2, inputHeight, inputHeight);

            //Initialized textures
            m_windowBackgroundTexture = GenerateVerticalGradientTexture(new Color[] { Color.black * 0.65f, Color.grey * 0.45f },
                                                                        new float[] { 0.95f, 0.7f },
                                                                        1, 400);
            m_titleBarTexture = GenerateVerticalGradientTexture(new Color[] { Color.grey * 0.85f, Color.grey * 0.55f},
                                                                        new float[] { 0.95f, 0.9f },
                                                                        1, 20);
            m_closeButtonTexture = GenerateVerticalGradientTexture(new Color[] { Color.red * 0.85f, Color.red * 0.75f },
                                                                        new float[] { 0.95f, 0.9f },
                                                                        1, 20);
            m_inputFieldInactiveTexture = new Texture2D(1, 1);
            m_inputFieldInactiveTexture.SetPixel(0, 0, m_inputActiveBackgroundColor);
            m_inputFieldInactiveTexture.Apply();
            m_inputFieldActiveTexture = new Texture2D(1, 1);
            m_inputFieldActiveTexture.SetPixel(0, 0, m_inputInactiveBackgroundColor);
            m_inputFieldActiveTexture.Apply();
            m_resizeAreaTexture = new Texture2D(1, 1);
            m_resizeAreaTexture.SetPixel(0, 0, m_inputActiveBackgroundColor);
            m_resizeAreaTexture.Apply();

            //Get cursor textures
            m_inputCursorTexture = Resources.Load<Texture2D>("CMD/Images/Cur_Input");
            m_resizeCursorTexture = Resources.Load<Texture2D>("CMD/Images/Cur_Resize");
            m_moveCursorTexture = Resources.Load<Texture2D>("CMD/Images/Cur_Move");
            m_clickCursorTexture = Resources.Load<Texture2D>("CMD/Images/Cur_Click");

            //Inizialize list of stored logs
            m_printCommands = new List<ConsolePrintInfo>();

            //Default state is disabled
            m_enabled = false;

            //Add eventlistener
            Application.logMessageReceivedThreaded -= OnLogMessageReceive;
            Application.logMessageReceivedThreaded += OnLogMessageReceive;

            if(m_methods == null)
                RefreshCommands();
        }
        /// <summary>
        /// Disable the console, use this to block end-users from using the console
        /// When disabled, Init will do nothing
        /// </summary>
        public static void Disable ()
        {
            m_forceDisable = true;
            Application.logMessageReceivedThreaded -= OnLogMessageReceive;
        }
        /// <summary>
        /// Enable the console
        /// </summary>
        public static void Enable ()
        {
            m_forceDisable = false;
            if(m_skin != null)
                Application.logMessageReceivedThreaded += OnLogMessageReceive;
        }

        /// <summary>
        /// Searches for commands in the projects Assembly. Is automaticly called on Init() if RefreshCommands() was has not been called yet
        /// </summary>
        /// <param name="assemblies">assemblies to search in. Defaults to the assembly of the CMD class</param>
        public static void RefreshCommands (Assembly[] assemblies = null)
        {
            //Default to current assembly
            if (assemblies == null)
                assemblies = new Assembly[1] { Assembly.GetAssembly(typeof(CMD)) };

            //Store start time
            float startTime = Time.realtimeSinceStartup;
            m_methods = new List<MethodInfo>();

            //Loop through every assembly and find commands
            for (int current = 0; current < assemblies.Length; current++)
            {
                MethodInfo[] methods = assemblies[current].GetTypes()
                  .SelectMany(t => t.GetMethods())
                  .Where(m => m.GetCustomAttributes(typeof(ConsoleCommand), false).Length > 0)
                  .ToArray();

                for (int i = 0; i < methods.Length; i++)
                {
                    //Static methdos only
                    if (!methods[i].IsStatic)
                        continue;
                    //Name must be CMD+(name) and name must be at least 1 characters, thats why >3 and not >=3
                    if (methods[i].Name.Length > 3)
                    {
                        //The command must begin with CMD
                        if (methods[i].Name[0] == 'C' && methods[i].Name[1] == 'M' && methods[i].Name[2] == 'D')
                        {
                            m_methods.Add(methods[i]);
                        }
                    }
                }
            }
            //Print refresh duration
            Log("Refreshed Commands in " + (Time.realtimeSinceStartup - startTime) + "ms");
        }
        #endregion

        #region Log Methods
        /// <summary>
        /// Logs a message to the console
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <returns>Info of the message</returns>
        public static ConsolePrintInfo Log (object message)
        {
            return CreatePrintInfo(LogType.Log, message.ToString());
        }
        /// <summary>
        /// Logs a warning to the console
        /// </summary>
        /// <param name="warning">Warning message</param>
        /// <returns>Info of the message</returns>
        public static ConsolePrintInfo Warning (string warning)
        {
            return CreatePrintInfo(LogType.Warning, warning);
        }
        /// <summary>
        /// Logs an error to the console. Stacktrace is automaticly added
        /// </summary>
        /// <param name="error">Error message</param>
        /// <returns>Info of the message</returns>
        public static ConsolePrintInfo Error (string error)
        {
            if (m_openConsoleOnError)
                ToggleConsole(true);
            return CreatePrintInfo(LogType.Error, error, Environment.StackTrace);
        }
        /// <summary>
        /// Logs an exception to the console. Stacktrace is automaticly added
        /// </summary>
        /// <param name="exception">Exception</param>
        /// <returns>Info of the message</returns>
        public static ConsolePrintInfo Error (Exception exception)
        {
            if (m_openConsoleOnError)
                ToggleConsole(true);
            return CreatePrintInfo(LogType.Exception, exception.ToString(), exception.StackTrace);
        }
        #endregion

        #region Graphical
        /// <summary>
        /// Generates a texture using a Unity gradient as color
        /// </summary>
        /// <param name="colors">the colors to use for the gradient</param>
        /// <param name="alphas">the alpha values to use for the gradient</param>
        /// <param name="width">Width of the texture (1 is recommended)</param>
        /// <param name="height">Height of the texture</param>
        /// <returns>Texture with a gradient</returns>
        public static Texture2D GenerateVerticalGradientTexture (Color[] colors, float[] alphas, int width, int height)
        {
            Texture2D tex = new Texture2D(width, height);
            Gradient gradient = new Gradient();

            //Alpha
            GradientAlphaKey[] aKeys = new GradientAlphaKey[alphas.Length];
            for (int i = 0; i < aKeys.Length; i++)
            {
                aKeys[i] = new GradientAlphaKey(alphas[i], 1f / aKeys.Length * i);
            }

            //Colors
            GradientColorKey[] cKeys = new GradientColorKey[colors.Length];
            for (int i = 0; i < cKeys.Length; i++)
            {
                cKeys[i] = new GradientColorKey(colors[i], 1f / cKeys.Length * i);
            }

            gradient.SetKeys(cKeys, aKeys);

            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    tex.SetPixel(x, y, gradient.Evaluate(y / (tex.height / 100f) / 100f));
                }
            }
            tex.Apply();

            return tex;
        }
        private static void UpdateCursor ()
        {
            Vector2 mousePosition = Input.mousePosition;
            mousePosition.y = Screen.height - mousePosition.y;
            if (Input.GetMouseButtonUp(0))
            {
                if (m_inputFieldRect.Contains(mousePosition))
                {
                    if (m_inputCursorTexture != null)
                        Cursor.SetCursor(m_inputCursorTexture, Vector2.zero, CursorMode.Auto);
                    m_isTyping = true;
                } else
                {
                    m_isTyping = false;
                }

                if (m_closeButtonRect.Contains(mousePosition) && m_closeButtonRect.Contains(m_mouseDownPosition))
                    ToggleConsole(false);

                EndDrag();
                EndResize();
            }
            if (Input.GetMouseButtonDown(0))
            {
                m_mouseDownPosition = mousePosition;
                if (m_titleRect.Contains(mousePosition))
                {
                    StartDrag();
                }
                if (m_resizeArea.Contains(mousePosition))
                {
                    StartResize();
                }
            }

            Texture2D cursorTex = null;
            Vector2 hitPoint = new Vector2(-666, -666);

            //Cursors
            //Input
            if (m_inputFieldRect.Contains(mousePosition))
                cursorTex = m_inputCursorTexture;

            //Resize
            if (m_resizeArea.Contains(mousePosition) || m_isResizing)
                cursorTex = m_resizeCursorTexture;

            //Drag
            if (m_titleRect.Contains(mousePosition) || m_isDragging)
                cursorTex = m_moveCursorTexture;

            if (m_closeButtonRect.Contains(mousePosition))
            {
                cursorTex = m_clickCursorTexture;
                hitPoint = new Vector2(2, 0);
            }

            if (hitPoint == new Vector2(-666, -666) && cursorTex != null)
                hitPoint = new Vector2(cursorTex.width / 2, cursorTex.height / 2);

            if (cursorTex != null)
                Cursor.SetCursor(cursorTex, hitPoint, CursorMode.ForceSoftware);
            else
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        #endregion

        #region Private Methods
        //Creating / Printing Commands
        private void Start ()
        {
            m_instance = this;
        }
        private static void OnLogMessageReceive (string condition, string stackTrace, LogType type)
        {
            CreatePrintInfo(type, condition, stackTrace);
        }
        private static ConsolePrintInfo CreatePrintInfo (LogType type = LogType.Log, string printContent = "", string stackTrace = "", Color color = default(Color))
        {
            ConsolePrintInfo info = new ConsolePrintInfo(type, printContent, stackTrace, false, color);

            if(m_printCommands != null)
                m_printCommands.Add(info);
            return info;
        }
        private void Update ()
        {
            if (m_forceDisable)
                return;
            
            if (Input.GetKeyUp(m_enableKeyCode))
            {
                ToggleConsole();
            }

            //Only continue of the console is enabled
            if (!m_enabled)
                return;

            //Update cursor texture
            UpdateCursor();

            //Typing logic
            if (m_isTyping)
            {
                if (Input.GetKey(KeyCode.Backspace))
                {
                    if (!m_usingBackspace)
                    {
                        if (m_inputString.Length > 0)
                            m_inputString = m_inputString.Remove(m_inputString.Length - 1);
                        m_usingBackspace = true;
                    }
                } else
                {
                    m_usingBackspace = false;
                    m_inputString += Input.inputString.Replace("`", "");
                }


                if (Input.GetKeyUp(KeyCode.Return))
                {
                    TryCommand();
                }
            }

            if (Input.GetKeyUp(KeyCode.UpArrow))
            {
                if (m_previousCommandIndex - 1 >= 0)
                    m_previousCommandIndex--;
                if (m_previousCommands.Count > 0 && m_previousCommandIndex < m_previousCommands.Count)
                    if (m_previousCommands[m_previousCommandIndex] != null)
                        m_inputString = m_previousCommands[m_previousCommandIndex].Trim();
            }
            if (Input.GetKeyUp(KeyCode.DownArrow))
            {
                if (m_previousCommandIndex + 1 < m_previousCommands.Count)
                {
                    m_previousCommandIndex++;
                    if (m_previousCommands.Count > 0 && m_previousCommandIndex < m_previousCommands.Count)
                        if (m_previousCommands[m_previousCommandIndex] != null)
                            m_inputString = m_previousCommands[m_previousCommandIndex].Trim();
                } else
                    m_inputString = "";
            }

            if (m_isDragging)
                OnDrag();
            if (m_isResizing)
                OnResize();
        }
        private MethodInfo FindCommand (string commandName)
        {
            for (int i = 0; i < m_methods.Count; i++)
            {
                if (m_methods[i].Name.Trim() == commandName.Trim())
                    return m_methods[i];
            }
            return null;
        }
        private static void ToggleConsole (bool state)
        {
            m_enabled = state;
            m_isTyping = m_enabled;
            m_inputString = "";
            EndDrag();
            EndResize();
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        private static void ToggleConsole ()
        {
            ToggleConsole(!m_enabled);
        }
        private void TryCommand ()
        {
            string[] splitCommand = m_inputString.Split(' ');
            if (splitCommand.Length > 0)
            {
                string commandName = "CMD" + splitCommand[0];
                try
                {
                    //Find Command
                    MethodInfo command = FindCommand(commandName);
                    if (command != null)
                    {
                        Log("~ " + command.Name.Remove(0, 3)).SetColor(new Color32(115, 255, 115, 255));
                        if (splitCommand.Length > 1)
                        {
                            string[] parameters = new string[splitCommand.Length - 1];
                            for (int i = 0; i < parameters.Length; i++)
                            {
                                parameters[i] = splitCommand[i + 1];
                            }

                            //Execute command
                            command.Invoke(this, parameters);
                        } else
                            command.Invoke(this, null);
                    } else
                    {
                        //No command was found
                        Log("~ The command \"" + commandName + "\" could not be found.").SetColor(new Color32(255, 115, 115, 255));
                    }
                    m_previousCommands.Add(m_inputString);
                } catch (Exception e)
                {
                    Error(e);
                }
            }
            m_inputString = "";
            m_previousCommandIndex = m_previousCommands.Count;
        }
        private void OnGUI ()
        {
            //This method needs heavy cleaning

            if (!m_enabled)
                return;
            
            Color startColor = GUI.contentColor;

            GUI.depth = -9999;
            GUI.skin = m_skin;

            //Title
            GUI.DrawTexture(m_titleRect, m_titleBarTexture);
            GUI.DrawTexture(m_resizeArea, m_resizeAreaTexture);
            GUI.DrawTexture(m_closeButtonRect, m_closeButtonTexture);
            Rect xRect = m_closeButtonRect;
            xRect.x += GUI.skin.label.CalcSize(new GUIContent("X")).x / 2;
            GUI.Label(xRect, "X");

            Rect titleRect = m_titleRect;
            titleRect.x += titleRect.width / 2 - GUI.skin.label.CalcSize(new GUIContent("Console")).x / 2;
            GUI.skin.label.fontSize = 12;
            GUI.Label(titleRect, "Console");
            GUI.skin.label.fontSize = m_fontSize;


            //Draw background textures
            GUI.DrawTexture(m_windowRect, m_windowBackgroundTexture);
            if (m_isTyping)
                GUI.DrawTexture(m_inputFieldRect, m_inputFieldActiveTexture);
            else
                GUI.DrawTexture(m_inputFieldRect, m_inputFieldInactiveTexture);

            //Draw input string
            GUI.contentColor = Color.white;
            Rect inputPosition = m_inputFieldRect;
            inputPosition.x += 2f;
            GUI.Label(inputPosition, m_inputString);
            GUI.contentColor = startColor;

            float relativeY = 0;
            float allowedHeight = m_windowRect.height / m_fontSize;

            //Draw stacktrace and store its height in pixels
            int lineNum = 0;
            for (int i = m_printCommands.Count; i-- > 0;)
            {
                if (m_printCommands[i].type == LogType.Error || m_printCommands[i].type == LogType.Exception)
                {
                    GUI.contentColor = new Color32(235, 115, 115, 255);
                    if (m_printCommands[i].stackTrace != "")
                    {
                        float stackHeight = GUI.skin.GetStyle("Label").CalcHeight(new GUIContent(m_printCommands[i].stackTrace), m_windowRect.width);
                        relativeY += stackHeight;

                        if (relativeY + lineNum * m_fontSize > m_windowRect.height)
                            break;

                        float y = (m_windowRect.y + m_windowRect.height) - (lineNum * m_fontSize) - (m_fontSize + 2);
                        GUI.Label(new Rect(m_windowRect.x, y, m_windowRect.width, m_fontSize), "[] " + m_printCommands[i].stackTrace);
                    }
                }

                //Values to be used
                Rect position = new Rect(m_windowRect.x, (m_windowRect.y + m_windowRect.height) - (lineNum * m_fontSize) - (m_fontSize + 2) - relativeY, m_windowRect.width, m_fontSize);
                string prefix = "";
                string message = m_printCommands[i].message;
                Color color = Color.white;

                //Apply prefix
                if (m_printCommands[i].type == LogType.Log)
                {
                    if (m_printCommands[i].color != default(Color))
                        color = m_printCommands[i].color;
                    prefix = "- ";
                } else if (m_printCommands[i].type == LogType.Error || m_printCommands[i].type == LogType.Exception)
                {
                    color = new Color32(235, 115, 115, 255);
                    prefix = "{!!} ";
                } else if (m_printCommands[i].type == LogType.Warning)
                {
                    color = new Color32(235, 115, 115, 255);
                    prefix = "|!| ";
                }

                //If outside of rect
                if (relativeY + lineNum * m_fontSize > m_windowRect.height - m_fontSize - 2)
                    break;

                //PRINT
                m_skin.GetStyle("Label").fontStyle = FontStyle.Bold;
                GUI.contentColor = color;
                GUI.Label(position, prefix + message);
                GUI.contentColor = startColor;
                m_skin.GetStyle("Label").fontStyle = FontStyle.Normal;

                //END
                lineNum++;

                relativeY += m_printCommands[i].lineCount * m_fontSize;
                if (m_printCommands[i].lineCount > 0)
                    relativeY += m_fontSize + 2;
                
            }
        }

        //Drag
        private static void StartDrag ()
        {
            m_isDragging = true;
            m_dragStartMousePosition = Input.mousePosition;
            m_dragStartWindowPosition = m_windowRect.position;
            m_dragStartTitlePosition = m_titleRect.position;
            m_dragStartInputPosition = m_inputFieldRect.position;
            m_dragStartClosePosition = m_closeButtonRect.position;
        }
        private static void OnDrag ()
        {
            Vector2 delta = m_dragStartMousePosition - Input.mousePosition;
            delta.y *= -1;
            
            m_windowRect.position = m_dragStartWindowPosition - delta;
            m_titleRect.position = m_dragStartTitlePosition - delta;
            m_inputFieldRect.position = m_dragStartInputPosition - delta;
            m_resizeArea.position = new Vector2(m_inputFieldRect.position.x + m_inputFieldRect.width, m_inputFieldRect.position.y);
            m_closeButtonRect.position = m_dragStartClosePosition - delta;
        }
        private static void EndDrag ()
        {
            m_isDragging = false;
            m_dragStartWindowPosition = Vector2.zero;
            m_dragStartMousePosition = Vector2.zero;
            m_dragStartTitlePosition = Vector2.zero;
            m_dragStartInputPosition = Vector2.zero;
            m_dragStartClosePosition = Vector2.zero;
        }

        //Resize
        private static void StartResize ()
        {
            m_isResizing = true;
            m_resizeStartMousePosition = Input.mousePosition;
            m_resizeStartWindowSize = m_windowRect.size;
        }
        private static void OnResize ()
        {
            Vector2 delta = m_resizeStartMousePosition - Input.mousePosition;
            delta.y *= -1;
            m_windowRect.size = m_resizeStartWindowSize - delta;

            //Clamp window
            if (m_windowRect.width < m_minimalSize.x)
                m_windowRect.width = m_minimalSize.x;
            if (m_windowRect.height < m_minimalSize.y)
                m_windowRect.height = m_minimalSize.y;

            float textSize = m_fontSize + 2;
            m_titleRect.size = new Vector2(m_windowRect.width - textSize - 2, m_titleRect.height);
            m_inputFieldRect.size = new Vector2(m_windowRect.width - textSize - 2, m_inputFieldRect.height);
            m_inputFieldRect.position = new Vector2(m_windowRect.x, m_windowRect.y + m_windowRect.height);
            m_resizeArea.position = new Vector2(m_inputFieldRect.x + m_inputFieldRect.width, m_inputFieldRect.y);
            m_closeButtonRect.x = m_titleRect.x + m_titleRect.width;
        }
        private static void EndResize ()
        {
            m_isResizing = false;
            m_resizeStartMousePosition = Vector2.zero;
            m_resizeStartWindowSize = Vector2.zero;
        }
        
        #endregion

        #region Properties
        /// <summary>
        /// The fontsize used to for the log entries
        /// </summary>
        public static int fontSize
        {
            set
            {
                m_fontSize = value;
                m_skin.GetStyle("Label").fontSize = value;
            }
            get
            {
                return m_fontSize;
            }
        }
        /// <summary>
        /// The size of the console's window
        /// </summary>
        public static Vector2 windowSize
        {
            set
            {
                //Create rectangles
                float inputHeight = m_fontSize + 2;
                float width = value.x;
                float height = value.y;

                m_windowRect = new Rect(0, Screen.height - height - inputHeight, width, height);
                m_titleRect = new Rect(0, Screen.height - height - inputHeight * 2, width, inputHeight);
                m_inputFieldRect = new Rect(0, Screen.height - inputHeight, width, inputHeight);
            }
            get
            {
                return m_windowRect.size;
            }
        }
        /// <summary>
        /// The key that enables the console
        /// </summary>
        public static KeyCode enableKeyCode
        {
            set
            {
                m_enableKeyCode = value;
            }
            get
            {
                return m_enableKeyCode;
            }
        }
        /// <summary>
        /// The poistion of the console
        /// </summary>
        public static Vector2 windowPosition
        {
            set
            {
                m_windowRect.position = value;
                m_titleRect.position = new Vector2(m_windowRect.x, m_windowRect.y);
                m_inputFieldRect.position = new Vector2(m_windowRect.x, m_windowRect.y + m_windowRect.height);
                m_resizeArea.position = new Vector2(m_windowRect.x + m_windowRect.width, m_windowRect.y + m_windowRect.height);
            }
            get
            {
                return m_windowRect.position;
            }
        }
        /// <summary>
        /// Should the console automaticly open when an error/excpetion is thrown
        /// </summary>
        public static bool openConsoleOnError
        {
            set
            {
                m_openConsoleOnError = value;
            }
            get
            {
                return m_openConsoleOnError;
            }
        }
        /// <summary>
        /// The texture used as background for the main window
        /// </summary>
        public static Texture2D windowTexture
        {
            get
            {
                return m_windowBackgroundTexture;
            }
            set
            {
                m_windowBackgroundTexture = value;
            }
        }
        #endregion
        //-----------------------------------------------------------------------//
        //CONSOLE COMMANDS
        //Commands do not have to be in this class for them to work, as long as they are in the same assembly
        //-----------------------------------------------------------------------//
        #region Console Commands
        //Public
        [ConsoleCommand("About the console")]
#pragma warning disable CS1591 
        public static void CMDAbout ()
        {
            Log("Unity Command Console | ver: " + Assembly.GetAssembly(typeof(CMD)).GetName().Version);
            Log("By: Daan Ruiter");
            Log("daanruiter.net");
            Log("contact@daanruiter.net");
        }
        [ConsoleCommand("How to use the console")]
#pragma warning disable CS1591 
        public static void CMDHelp ()
        {
            Log("To create your own console command, place the attribute ConsoleCommand above your method.");
            Log("Make sure your method is both <b>public</b> & <b>static</b>.");
            Log("You need to give a reference to your assembly to the console for it to be able to find your command.");
            Log("The easiest way to do this is by adding the following line before or after calling CMD.Init():");
            Log("<b>CMD.RefreshCommands(new Assembly[] { Assembly.GetAssembly(typeof(<b>SCRIPT_NAME</b>)) } );</b>").SetColor(Color.cyan * 0.89f);
            Log("Replace <b>SCRIPT_NAME</b> with any script in your project.");
            Log("Done! You can now use the console and call your custom commands.");
            Log("You can also customize some of the visuals of the console, use IntelliSense to find out what.");
            Log("You can send messages and print values to your console using either CMD.Log(), CMD.Warning() or CMD.Error().");
            Log("Have any other questions/suggestions? contact me at <i>http://daanruiter.net/contact.php</i>");
        }
        [ConsoleCommand("Hello!")]
#pragma warning disable CS1591 
        public static void CMDHello ()
        {
            Log("Hello World!");
        }
        [ConsoleCommand("Completely clears the console")]
#pragma warning disable CS1591 
        public static void CMDClear ()
        {
            m_printCommands.Clear();
        }
        [ConsoleCommand("List all non hidden commands")]
#pragma warning disable CS1591 
        public static void CMDList ()
        {
            for (int i = 0; i < m_methods.Count; i++)
            {
                object[] attributes = m_methods[i].GetCustomAttributes(true);
                bool hidden = false;
                string description = "";
                for (int a = 0; a < attributes.Length; a++)
                {
                    if (attributes[a].GetType() == typeof(ConsoleCommand))
                        if ((attributes[a] as ConsoleCommand).hidden)
                        {
                            hidden = true;
                            break;
                        } else
                        {
                            description = (attributes[a] as ConsoleCommand).description;
                        }
                }
                if (hidden)
                    continue;

                string commandName = m_methods[i].Name;
                commandName = commandName.Remove(0, 3);

                string final = "<b>" + commandName + "</b>";

                ParameterInfo[] parameters = m_methods[i].GetParameters();
                if (parameters.Length > 0)
                {
                    final += " (";
                    for (int p = 0; p < parameters.Length; p++)
                    {
                        final += parameters[p].Name;
                        if (p + 1 < parameters.Length)
                            final += ", ";
                    }
                    final += ")";
                }
                if (description != "")
                    final += "<size=11><color=#7788aa> " + description + "</color></size>";

                Log(final);
            }
        }
        [ConsoleCommand("List all non hidden commands without any markup or descriptions")]
#pragma warning disable CS1591 
        public static void CMDListRaw ()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            for (int i = 0; i < m_methods.Count; i++)
            {
                object[] attributes = m_methods[i].GetCustomAttributes(true);
                bool hidden = false;
                for (int a = 0; a < attributes.Length; a++)
                {
                    if (attributes[a].GetType() == typeof(ConsoleCommand))
                        if ((attributes[a] as ConsoleCommand).hidden)
                        {
                            hidden = true;
                            break;
                        } 
                }
                if (hidden)
                    continue;

                Log(m_methods[i].DeclaringType.ToString() + " " + m_methods[i]);
            }
        }
        [ConsoleCommand("Quit the current application")]
#pragma warning disable CS1591 
        public static void CMDQuit ()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            Application.Quit();
        }
        [ConsoleCommand("Reset the position & size of the console")]
#pragma warning disable CS1591 
        public static void CMDReset ()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            //Get console window size
            float width = Screen.width / 100f * m_widthPercentage;
            float height = Screen.height / 100f * m_heightPercentage;
            float inputHeight = m_fontSize + 2;

            //Create rectangles
            m_windowRect = new Rect(0, Screen.height - height - inputHeight, width, height);
            m_titleRect = new Rect(0, Screen.height - height - inputHeight * 2, width - inputHeight, inputHeight);
            m_inputFieldRect = new Rect(0, Screen.height - inputHeight, width - inputHeight, inputHeight);
            m_resizeArea = new Rect(width - inputHeight, Screen.height - inputHeight, inputHeight, inputHeight);
            m_closeButtonRect = new Rect(width - inputHeight, Screen.height - height - inputHeight * 2, inputHeight, inputHeight);
        }

        //Hidden
        [ConsoleCommand("Hack the matrix", true)]
#pragma warning disable CS1591 
        public static void CMDMatrix ()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            CMDClear();
            string line = "";
            for (int l = 0; l < (int)(m_windowRect.height / m_fontSize); l++)
            {
                for (int c = 0; c < (int)(m_windowRect.width / m_fontSize); c++)
                {
                    line += GetRandomString();
                }
            }
            Log(line).SetColor(Color.green);
            m_instance.RefreshMatrix();
        }
        [ConsoleCommand("Stop hacking the matrix", true)]
#pragma warning disable CS1591 
        public static void CMDStopMatrix ()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            m_instance.StopMatrix();
            Log("Welcome to the real world.").SetColor(Color.green);
        }

        #region Matrix
        private void Matrix ()
        {
            CMDClear();
            string line = "";
            for (int l = 0; l < (int)(m_windowRect.height / m_fontSize) / 2; l++)
            {
                for (int c = 0; c < (int)(m_windowRect.width / m_fontSize); c++)
                {
                    line += GetRandomString();
                }
            }
            Log(line).SetColor(Color.green);
            m_instance.RefreshMatrix();
        }

        private void StopMatrix ()
        {
            StopAllCoroutines();
            CancelInvoke();
            CMDClear();
        }
        private void RefreshMatrix ()
        {
            StartCoroutine(FrameEnum());
            Invoke("Matrix", 0.085f);
        }
        private IEnumerator FrameEnum ()
        {
            yield return new WaitForEndOfFrame();
        }
        private static string GetRandomString ()
        {
            string[] characters = new string[] { "hack", "b", "c", "d", "e", "f", "g", "{", "I", "j", "k", "l", "m", "}", "o", "p", "q", "r", "s", "t", "u", "\n", "w", "x", "y", "z"
                                           , "0", "1", "2", "-", "4", "5", "6", "7", "8", "9", "-", " ", "HELP", "!", "NEO", "%", "#", "RABBIT"};
            return characters[UnityEngine.Random.Range(0, characters.Length)];
        }
        #endregion
        #endregion
    }
}