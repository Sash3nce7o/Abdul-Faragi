using ClickableTransparentOverlay;
using Swed64;
using ImGuiNET;
using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.InteropServices;
namespace Counter_Strike_multihack
{
     class Program : Overlay
    {

        [DllImport("user32.dll")]

        static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        public RECT GetWindowRect(IntPtr hWnd)
        {
            RECT rect = new RECT();
            GetWindowRect(hWnd, out rect);
            return rect;
        }




        Swed swed = new Swed("cs2");
        Offsets offsets = new Offsets();
        ImDrawListPtr drawlist;

        Entity localPlayer = new Entity();
        List<Entity> entities = new List<Entity>();
        List<Entity> enemyTeam = new List<Entity>();
        List<Entity> playerTeam = new List<Entity>();

        IntPtr client;

        Vector4 teamColor = new Vector4(0, 0, 1, 1); // RGBA, blue teamates
        Vector4 enemyColor = new Vector4(1, 0, 0, 1); // enemy red
        Vector4 healthBarColor = new Vector4(0, 1, 0, 1); // green
        Vector4 healthTextColor = new Vector4(0, 0, 0, 1); // black


        Vector2 windowLocation = new Vector2(0, 0);
        Vector2 windowSize = new Vector2(1920, 1080);
        Vector2 lineOrigin = new Vector2(1920/2, 1080);
        Vector2 windowCenter = new Vector2(1920 / 2, 1080 / 2);

        // ImGui checkbox

        bool enableEsp = true;
        
        bool enableTeamLine=true;
        bool enableTeamBox = true;
        bool enableTeamDot = false;
        bool enableTeamHealthBar =true;
        bool enableTeamDistance = true;

        bool enableEnemyLine = true;
        bool enableEnemyBox = true;
        bool enableEnemyDot = false;
        bool enableEnemyHealthBar = true;
        bool enableEnemyDistance = true;
        

        protected override void Render()
        {
            Drawmenu();
            DrawOverlay();
            Esp();
            ImGui.End();
        }

        void Esp()
        {

            drawlist = ImGui.GetWindowDrawList();

            if(enableEsp)
            {
                try
                {
                    foreach(var entity in entities)
                    {
                        if(entity.teamNum == localPlayer.teamNum)
                        {
                            DrawVisuals(entity, teamColor, enableTeamLine, enableTeamBox, enableTeamDot, enableTeamHealthBar, enableTeamDistance);
                            
                        }
                        else
                        {
                            DrawVisuals(entity, enemyColor, enableEnemyLine, enableEnemyBox, enableEnemyDot, enableEnemyHealthBar, enableEnemyDistance);
                        }
                    }
                }catch { }
            }
        }

        void DrawVisuals(Entity entity, Vector4 color, bool line, bool box, bool dot, bool healthbar, bool distance)
        {
            //check if 2d position valid

            if (IsPixelInsideScreen(entity.originScreenPosition))
            {

                // convert out of colors to units
                uint uintColor = ImGui.ColorConvertFloat4ToU32(color);
                uint uintHealthTextColor = ImGui.ColorConvertFloat4ToU32(healthTextColor);
                uint uinthealthBarColor = ImGui.ColorConvertFloat4ToU32(healthBarColor);

                //calculate box attributies
                Vector2 boxWidth = new Vector2((entity.originScreenPosition.Y - entity.absScreenPosition.Y) / 2, 0f); // divide height by 2 to simulate width
                Vector2 boxStart = Vector2.Subtract(entity.absScreenPosition, boxWidth);
                Vector2 boxEnd = Vector2.Add(entity.originScreenPosition, boxWidth);

                // calculate health barr

                float barPercent = entity.health / 100f;
                Vector2 barHeight = new Vector2(0, barPercent* (entity.originScreenPosition.Y - entity.absScreenPosition.Y));
                Vector2 barStart = Vector2.Subtract(Vector2.Subtract(entity.originScreenPosition, boxWidth), barHeight);
                Vector2 barEnd = Vector2.Subtract(entity.originScreenPosition, Vector2.Add(boxWidth, new Vector2(-4, 0)));

                // draw
                if (line)
                {
                    drawlist.AddLine(lineOrigin,entity.originScreenPosition, uintColor, 3); // draw line to feet of entities
                }

                if (box)
                {
                    drawlist.AddRect(boxStart, boxEnd, uintColor, 3); // box around character
                }
                if (dot)
                {
                    drawlist.AddCircleFilled(entity.originScreenPosition, 5, uintColor);
                }
                if (healthbar)
                {
                    drawlist.AddText(entity.originScreenPosition, uintHealthTextColor, $"hp: {entity.health}");
                    drawlist.AddRectFilled(barStart, barEnd, uinthealthBarColor);
                }
            }
        }

        bool IsPixelInsideScreen(Vector2 pixel)
        {
            return pixel.X > windowLocation.X && pixel.X < windowLocation.X + windowSize.X && pixel.Y > windowLocation.Y && pixel.Y < windowSize.Y + windowLocation.Y; // check all window bounds
        }

        ViewMatrix ReadMatrix(IntPtr matrixAddress)
        {
            var viewMatrix = new ViewMatrix();
            var floatMatrix = swed.ReadMatrix(matrixAddress);


            viewMatrix.m11 = floatMatrix[0];
            viewMatrix.m12 = floatMatrix[1];
            viewMatrix.m13 = floatMatrix[2];
            viewMatrix.m14 = floatMatrix[3];

            viewMatrix.m21 = floatMatrix[4];
            viewMatrix.m22 = floatMatrix[5];
            viewMatrix.m23 = floatMatrix[6];
            viewMatrix.m24 = floatMatrix[7];

            viewMatrix.m31 = floatMatrix[8];
            viewMatrix.m32 = floatMatrix[9];
            viewMatrix.m33 = floatMatrix[10];
            viewMatrix.m34 = floatMatrix[11];

            viewMatrix.m41 = floatMatrix[12];
            viewMatrix.m42 = floatMatrix[13];
            viewMatrix.m43 = floatMatrix[14];
            viewMatrix.m44 = floatMatrix[15];

            return viewMatrix;
        }

        Vector2 WorldToScreen(ViewMatrix matrix, Vector3 pos, int width, int height)
        {
            Vector2 screenCoordinates = new Vector2();

            //calculate screenW

            float screenW = (matrix.m41 * pos.X) + (matrix.m42 * pos.Y) + (matrix.m43 * pos.Z)+ matrix.m44;

            if (screenW > 0.001f) //check that entity is in front of us
            {
                // calculate X

                float screenX = (matrix.m11 * pos.X) + (matrix.m12 * pos.Y) +(matrix.m13 * pos.Z) + matrix.m14;

                // calculate Y

                float screenY = (matrix.m21 * pos.X) + ( matrix.m22 * pos.Y)+(matrix.m23 * pos.Z) +matrix.m24;

                //calculate camera center

                float camX = width / 2;
                float camY = height / 2;

                // perform perspective division and transformation

                float X = camX + (camX * screenX / screenW);
                float Y = camY - (camY * screenY / screenW);

                // return x and y

                screenCoordinates.X = X;
                screenCoordinates.Y = Y;
                return screenCoordinates;

            }
            else // return out of bounds vector if not front of us
            {
                return new Vector2(-99, -99);
            }
        }

        void Drawmenu()
        {
            ImGui.Begin("Counter-Strike 2 Multi Hack");
            if (ImGui.BeginTabBar("Tabs"))
            {
                //first page
                if (ImGui.BeginTabItem("General"))
                {
                    ImGui.Checkbox("Esp", ref enableEsp);
                    ImGui.EndTabItem();
                }
                //second page
                if (ImGui.BeginTabItem("Colors"))
                {

                    // Team Colors
                    ImGui.ColorPicker4("Team color", ref teamColor);
                    ImGui.Checkbox("Team line", ref enableTeamLine);
                    ImGui.Checkbox("Team box", ref enableTeamBox);
                    ImGui.Checkbox("Team dot", ref enableTeamDot);
                    ImGui.Checkbox("Team Healthbar", ref enableTeamHealthBar);

                    // Enemy Colors
                    ImGui.ColorPicker4("Enemy color", ref enemyColor);
                    ImGui.Checkbox("Enemy line", ref enableEnemyLine);
                    ImGui.Checkbox("Enemy box", ref enableEnemyBox);
                    ImGui.Checkbox("Enemy dot", ref enableEnemyDot);
                    ImGui.Checkbox("Enemy Healthbar", ref enableEnemyHealthBar);

                    ImGui.EndTabItem();




                }
            }
            ImGui.EndTabBar();

        }

        void DrawOverlay()
        {
            ImGui.SetNextWindowSize(windowSize);
            ImGui.SetNextWindowPos(windowLocation);
            ImGui.Begin("overlay", ImGuiWindowFlags.NoDecoration
                | ImGuiWindowFlags.NoBackground
                | ImGuiWindowFlags.NoBringToFrontOnFocus
                | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoInputs
                | ImGuiWindowFlags.NoCollapse
                | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse
                );
        }


        void MainLogic()
        {


            //calculate window position and size
            var window = GetWindowRect(swed.GetProcess().MainWindowHandle);
            windowLocation = new Vector2(window.left,window.top);
            windowSize = Vector2.Subtract(new Vector2(window.right, window.bottom), windowLocation);
            lineOrigin = new Vector2(windowLocation.X + windowSize.X / 2, window.bottom);
            windowCenter = new Vector2(lineOrigin.X, window.bottom - windowSize.Y /2 );


            client = swed.GetModuleBase("client.dll");

            

            while (true) //always run
            {
                RealoadEntities();
                Thread.Sleep(3);
                
                
            }
        }
        void RealoadEntities()
        {
            entities.Clear();
            playerTeam.Clear();
            enemyTeam.Clear();
            localPlayer.address = swed.ReadPointer(client, offsets.localPlayer); // set the address so we can update
            UpdateEntity(localPlayer); //update
            UpdateEntities();
        }

        void UpdateEntities() //handle all other entities
        {
            for (int i = 0; i < 64; i++) // normally less then 64 ents
            {
                IntPtr tempEntityAdress = swed.ReadPointer(client, offsets.entityList + i * 0x08);
                if (tempEntityAdress == IntPtr.Zero)
                    continue;
                
                Entity entity = new Entity();
                entity.address = tempEntityAdress;
                UpdateEntity(entity);
                if (entity.health < 1 || entity.health > 100)
                    continue; // another check but now if entity is dead
                if (!entities.Any(element => element.origin.X == entity.origin.X)) // check if there is a duplicate of the entity
                {
                    entities.Add(entity);

                    if(entity.teamNum == localPlayer.teamNum)
                    {
                        playerTeam.Add(entity);
                    }
                    else
                    {
                        enemyTeam.Add(entity);
                    }
                }
            }
        }

        void UpdateEntity(Entity entity)
        {

            //1d
            entity.health = swed.ReadInt(entity.address, offsets.health);
            entity.origin = swed.ReadVec(entity.address, offsets.origin);
            entity.teamNum = swed.ReadInt(entity.address, offsets.teamNum);




            //3d
            entity.origin = swed.ReadVec(entity.address, offsets.origin);

            entity.viewOffset = new Vector3(0, 0, 65); // simulate view offset

            entity.abs = Vector3.Add(entity.origin, entity.viewOffset);

            //2d

            var currentViewmatrix = ReadMatrix(client + offsets.viewMatrix);
            entity.originScreenPosition = Vector2.Add(WorldToScreen(currentViewmatrix, entity.origin, (int)windowSize.X, (int)windowSize.Y), windowLocation);
            entity.absScreenPosition = Vector2.Add(WorldToScreen(currentViewmatrix, entity.abs, (int)windowSize.X, (int)windowSize.Y), windowLocation);
        }


        static void Main(string[] args)
        {
            // run logic methotds and more

            Program program = new Program();
            program.Start().Wait();
            Thread mainLogicThread = new Thread(program.MainLogic) { IsBackground = true };
            mainLogicThread.Start();

        }
    }
}
