﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//0x1198C10

namespace ConsoleApplication11 //Decompile Game Logic Table
{
    enum BlockStatus { start, used, end, discovered };
    enum BranchType { NaB, DoWhile, While, If, Break, ToDefine, Else, GoTo }; //NaB = Not a Branch
    class PointerCouple
    {
        public int Pointers = 0, Logic = 0;
    }
    class Arguments {
        public byte[] Data;
        public int start;
        public int bank;
        public Block CurrBlock;
        public Block Block0;
        public List<string> tmp;
        public List<int> DataList;
        public int numblock;
        public bool[] visitedBlock;
        public List<int> returningStack;
        public Arguments(byte[] Data, int start, int bank, Block CurrBlock, Block Block0, List<string> tmp, List<int> DataList, int numblock, bool[] visitedBlock, List<int> returningStack) {
            this.Data = Data; this.start = start; this.bank = bank; this.CurrBlock = CurrBlock; this.Block0 = Block0; this.tmp = tmp; this.DataList = DataList; this.numblock = numblock; this.visitedBlock = visitedBlock; this.returningStack = returningStack;
        }
    }
    class LogicBlock
    {
        private BlockNode start, end;

        public LogicBlock(BlockNode start, BlockNode end)
        {
            Start = start;
            End = end;
        }

        internal BlockNode End
        {
            get
            {
                return end;
            }

            set
            {
                end = value;
            }
        }

        internal BlockNode Start
        {
            get
            {
                return start;
            }

            set
            {
                start = value;
            }
        }
    }
    class Cycle : LogicBlock
    {
        private BlockNode exit, realStart, realestStart;
        private bool doWhile;
        public Cycle(BlockNode start, BlockNode realStart, BlockNode realestStart, BlockNode end, BlockNode exit, List<BlockNode> LinearizedGraph, bool doWhile) : base(start, end)
        {
            Exit = exit;
            DoWhile = DoWhile;
            RealStart = realStart;
            RealestStart = realestStart;
            UpdateCycleFrom(LinearizedGraph);
        }

        private void UpdateCycleFrom(List<BlockNode> LinearizedGraph)
        {
            for (int i = Start.IndexInLinearizedGraph; i < Exit.IndexInLinearizedGraph; i++)
            {
                LinearizedGraph[i].CycleIn = this;
            }
        }

        internal BlockNode Exit
        {
            get
            {
                return exit;
            }

            set
            {
                exit = value;
            }
        }

        public bool DoWhile
        {
            get
            {
                return doWhile;
            }

            set
            {
                doWhile = value;
            }
        }

        internal BlockNode RealStart
        {
            get
            {
                return realStart;
            }

            set
            {
                realStart = value;
            }
        }

        internal BlockNode RealestStart
        {
            get
            {
                return realestStart;
            }

            set
            {
                realestStart = value;
            }
        }
    }
    class Else : LogicBlock
    {
        private BlockNode exit;
        public Else(BlockNode start, BlockNode end, BlockNode exit, List<BlockNode> LinearizedGraph) : base(start, end)
        {
            Exit = exit;
            Start.ElseIn = this;
            End.ElseIn = this;
        }

        private void UpdateElseFrom(List<BlockNode> LinearizedGraph)
        {
            for (int i = Start.IndexInLinearizedGraph; i < Exit.IndexInLinearizedGraph; i++)
            {
                LinearizedGraph[i].ElseIn = this;
            }
        }

        internal BlockNode Exit
        {
            get
            {
                return exit;
            }

            set
            {
                exit = value;
            }
        }
    }
    class Utilities
    {
        public static string ToHex(int a, int numbers)
        {
            string tmp = "";
            for (int i = 0; i < numbers; i++)
            {
                int tmp2 = (a >> ((numbers - 1 - i) * 4)) % 0x10;
                switch (tmp2)
                {
                    case 0xA:
                        tmp += "A";
                        break;
                    case 0xB:
                        tmp += "B";
                        break;
                    case 0xC:
                        tmp += "C";
                        break;
                    case 0xD:
                        tmp += "D";
                        break;
                    case 0xE:
                        tmp += "E";
                        break;
                    case 0xF:
                        tmp += "F";
                        break;
                    default:
                        tmp += tmp2.ToString();
                        break;
                };
            }
            return tmp;
        }
        public static int ToInt4Bytes(byte[] Data, int address) //Read bytes
        {
            return (Data[address]) + (Data[address + 1] << 8) + (Data[address + 2] << 16) + (Data[address + 3] << 24);
        }
        public static int ToInt3Ints(int I1, int I2, int I3) //Read ints
        {
            return (I1) + (I2 << 8) + (I3 << 16);
        }
        public static int ToInt3IntsSigned(int I1, int I2, int I3) //Read ints
        {
            return (((I1) + (I2 << 8) + (I3 << 16)) << 8) >> 8;

        }
        public static int ToInt2Ints(int I1, int I2) //Read ints
        {
            return (I1) + (I2 << 8);
        }
        public static int ToInt2IntsSigned(int I1, int I2) //Read ints
        {
            return (((I1) + (I2 << 8)) << 16) >> 16;

        }
        public static short ToInt2Bytes(byte[] Data, int address) //Read bytes
        {
            return (short)((Data[address]) + (Data[address + 1] << 8));
        }
        public static int ToInt3Bytes(byte[] Data, int address) //Read bytes
        {
            return (Data[address]) + (Data[address + 1] << 8) + (Data[address + 2] << 16);
        }
    }
    class SingleCommand
    {
        private int commandType, x, y, z, numParam, data, labelsNum, pointedEntry;
        private String command;
        public const int ImpossibleData = 0x1000000;

        public SingleCommand(int CT, int X, int Y, int Z)
        {
            CommandType = CT;
            this.X = X;
            this.Y = Y;
            this.Z = Z;
            NumParam = 0;
            Data = ImpossibleData; //Impossible default value
        }

        public SingleCommand(int CT, int X, int Y, int Z, Block Block0, Block CurrBlock) : this(CT, X, Y, Z)
        {
            Command = TranslateToText(Block0, CurrBlock);
        }

        public SingleCommand(int CT, int X, int Y, int Z, Block Block0, Block CurrBlock, BlockNode Zone, FullGraph Graph, int[] Labels, int LabelsNum) : this(CT, X, Y, Z)
        {
            this.LabelsNum = LabelsNum;
                Command = TranslateToText(Block0, CurrBlock, Zone, Graph, Labels);
        }
        
        override public String ToString()
        {
            if (Command == null)
            {
                return "[" + Utilities.ToHex(CommandType, 2) + " " + Utilities.ToHex(x, 2) + " " + Utilities.ToHex(y, 2) + " " + Utilities.ToHex(z, 2) + "]";
            }
            else return Command;
        }

        public static SingleCommand GetSingleCommand(Byte[] Data, int start, Block Block0, Block CurrBlock, List<int> DataList, List<String> tmp)
        {
            SingleCommand c = new SingleCommand(Data[start], Data[start + 1], Data[start + 2], Data[start + 3], Block0, CurrBlock);
            if (c.CommandType == 5)
                c.PointedEntry = Block0.Number[Utilities.ToInt2Ints(c.Y, c.Z)];
            if (c.CommandType == 7 || c.CommandType == 0xC || c.CommandType == 0xD)
                c.PointedEntry = CurrBlock.Number[Utilities.ToInt2Ints(c.Y, c.Z)];
            string t = c.ToString();
            t = c.InsertPreviousCommand(DataList, t, tmp, false);
            tmp.Add(t);
            DataList.Add(c.Data);
            return c;
        }

        public static SingleCommand GetSingleCommand(Byte[] Data, int start, Block Block0, Block CurrBlock, List<int> DataList, List<String> tmp, BlockNode Zone, FullGraph Graph, int[] Labels, int LabelsNum)
        {
            SingleCommand c = new SingleCommand(Data[start], Data[start + 1], Data[start + 2], Data[start + 3], Block0, CurrBlock, Zone, Graph, Labels, LabelsNum);
            string t = c.ToString();
            t = c.InsertPreviousCommand(DataList, t, tmp, true);
            if(t != "")
                t += Environment.NewLine;
            tmp.Add(t);
            DataList.Add(c.Data);
            return c;
        }

        private String TranslateToText(Block Block0, Block CurrBlock)
        {
            switch (CommandType)
            {
                case 1: //Pushes to stack
                    Data = Utilities.ToInt3IntsSigned(X, Y, Z);
                    return "[PUSH " + Data + "]";
                case 0:
                    return "[GET " + Utilities.ToInt2IntsSigned(Y, Z) + ", " + X + " FROM STACK]";
                case 3:
                    return "[COPY " + Utilities.ToInt2Ints(Y, Z) + ", " + X + " TO STACK]";
                case 9:
                    return "[END " + Utilities.ToInt3Ints(X, Y, Z) + "]";
                case 0xB:
                    return "[INCREMENT STACK POINTER BY " + Utilities.ToInt3Ints(X, Y, Z) + "]";
                case 0x5:
                    return "[JUMP TO 0-" + Block0.Number[Utilities.ToInt2Ints(Y, Z)] + ", " + X + "]";
                case 0x7:
                    return "[JUMP TO " + CurrBlock.BlockNum + "-" + CurrBlock.Number[Utilities.ToInt2Ints(Y, Z)] + ", " + X + "]";
                case 0xD:
                    NumParam = 1;
                    return "(IF $1 == 0, GO TO " + CurrBlock.BlockNum + "-" + CurrBlock.Number[Utilities.ToInt2Ints(Y, Z)] + ")";
                case 6:
                    return "[RETURN AND DECREMENT STACK POINTER BY " + Utilities.ToInt2Ints(Y, Z) + ", " + X + "]";
                case 8:
                    return "[RETURN " + Utilities.ToInt2Ints(Y, Z) + ", " + X + "]";
                case 0xC:
                    return "[GO TO " + CurrBlock.BlockNum + "-" + CurrBlock.Number[Utilities.ToInt2Ints(Y, Z)] + "]";
                case 4: // 04 extended codes
                    NumParam = ExtendedCodeParams.ExtCodeParams[Utilities.ToInt2Ints(Y, Z)];
                    switch (Utilities.ToInt2Ints(Y, Z))
                    {
                        case 0x00:
                            return "(DELAY PARSING BY $1)";
                        case 0x03:
                            return "(LOAD SCRIPT " + CurrBlock.BlockNum + "-$1)";
                        case 0x0A:
                            return "(SET EVENT FLAG $2 TO $1)";
                        case 0x0B:
                            return "(SET BYTE SRAM1 + $2 TO $1)";
                        case 0x0E:
                            return "(LOAD BYTE SRAM1 + $1)";
                        case 0x0C:
                            return "(SET BYTE SRAM2 + $2 TO $1)";
                        case 0x0D:
                            return "(PUSH EVENT FLAG $1)";
                        case 0x1E:
                            return "(ADD CHAR $1)";
                        case 0x1F:
                            return "(REMOVE CHAR $1)";
                        case 0x33: //Display from bank 0
                            return "(DISPLAY 0-$1, $2, ^$3)";
                        case 0x32: //Display from bank we're in
                            if(CurrBlock.BlockNum == 0)
                                return "(DISPLAY CALLER-$1, $2, ^$3)";
                            return "(DISPLAY " + CurrBlock.BlockNum + "-$1, $2, ^$3)";
                        case 0x36:
                            return "(PUSH MENU SELECTION)";
                        case 0x77:
                            return "(STOP SCREEN SHAKING)";
                        case 0x83:
                            return "(PLAY SOUND $2, $1)";
                        case 0x8C:
                            return "(START BATTLE AGAINST $1)";
                        case 0xA0:
                            return "(OPEN UP SHOP MENU $2, $1)";
                        case 0xA1:
                            return "(OPEN UP SAVING MENU)";
                        case 0xA2:
                            return "(OPEN UP NAMING MENU $1)";
                        case 0xA3:
                            return "(CHECK $1'S FAKE NAME)";
                        case 0xA4:
                            return "(OPEN ITEM GUY MENU)";
                        case 0xA5:
                            return "(OPEN MONEY MENU)";
                        case 0xAA:
                            return "(RESTART THE GAME)";
                        case 0xB5:
                            return "(LOAD SPRITE TABLE $1)";
                        case 0xEC:
                            return "(PUSH CHARACTER $1'S LEVEL)";
                        case 0xF6:
                            return "(START STAFF ROLL)";
                        default:
                            string tmp = "(" + Utilities.ToHex(CommandType, 2) + " " + Utilities.ToHex(x, 2) + " " + Utilities.ToHex(y, 2) + " " + Utilities.ToHex(z, 2);
                            if (NumParam > 0)
                                tmp += " |";
                            for (int i = 0; i < NumParam; i++)
                            {
                                tmp += " $" + (i + 1);
                                if (i != NumParam - 1)
                                    tmp += ",";
                            }
                            return tmp + ")";
                    }
                case 0xE: // 0E extended Math codes
                    switch (X)
                    {
                        case 0x00: //-$1
                            NumParam = 1;
                            return "(PUSH -$1)";
                        case 0x1://$1+$2
                            NumParam = 2;
                            return "(PUSH $2 + $1)";
                        case 0x2://$2-$1
                            NumParam = 2;
                            return "(PUSH $2 - $1)";
                        case 0x3://$1*$2
                            NumParam = 2;
                            return "(PUSH $2 * $1)";
                        case 0x4://$2/$1
                            NumParam = 2;
                            return "(PUSH $2 / $1)";
                        case 0x5://$2%$1
                            NumParam = 2;
                            return "(PUSH $2 % $1)";
                        case 0x6://$1++
                            NumParam = 1;
                            return "(PUSH $1 + 1)";
                        case 0x7://$1--
                            NumParam = 1;
                            return "(PUSH $1 - 1)";
                        case 0x8://$1&$2
                            NumParam = 2;
                            return "(PUSH $1 & $2)";
                        case 0x9://$1|$2
                            NumParam = 2;
                            return "(PUSH $1 | $2)";
                        case 0xA:// If $1 == $2
                            NumParam = 2;
                            return "(PUSH $1 == $2)";
                        case 0xB:// If $1 != $2
                            NumParam = 2;
                            return "(PUSH $1 != $2)";
                        case 0xC:// If $1 > $2
                            NumParam = 2;
                            return "(PUSH $1 > $2)";
                        case 0xD:// If $1 < $2
                            NumParam = 2;
                            return "(PUSH $1 < $2)";
                        case 0xE:// If $1 >= $2
                            NumParam = 2;
                            return "(PUSH $1 >= $2)";
                        case 0xF:// If $1 <= $2
                            NumParam = 2;
                            return "(PUSH $1 <= $2)";
                        case 0x10:// Copy $1
                            NumParam = 1;
                            return "(PUSH $1 AGAIN)";
                        case 0x11:
                            return "[DECREMENT STACK POINTER BY 1]";
                        case 0x12:
                            return "[DECREMENT STACK POINTER BY 1]";
                        case 0x13:
                            return "[NOP]";
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
            return null;
        }

        private String TranslateToText(Block Block0, Block CurrBlock, BlockNode Zone, FullGraph Graph, int[] Labels)
        {
            switch (CommandType)
            {
                case 0x5:
                    return "[JUMP TO 0-" + Block0.GraphAssociatedToBlock[Utilities.ToInt2Ints(Y, Z)] + ", " + X + "]";
                case 0x7:
                    return "[JUMP TO " + CurrBlock.BlockNum + "-" + CurrBlock.GraphAssociatedToBlock[Utilities.ToInt2Ints(Y, Z)] + ", " + X + "]";
                case 0xD:
                    NumParam = 1;
                    if (Zone.CycleIn != null && Zone.CycleIn.Start == Zone)
                    {
                        if (!Zone.CycleIn.DoWhile) 
                            return "(WHILE($1 == 0))";
                        //tmp.Add("while($1) ");
                    }
                    if (Zone.CycleIn != null && Zone.CycleIn.End == Zone)
                    {
                        if (Zone.CycleIn.DoWhile)
                            return "(WHILE($1 == 0));";
                        //tmp.Add("while($1) ");
                    }
                    if (Zone.LChild == null)
                    {
                        int Branchy = Utilities.ToInt2Ints(Y, Z);
                        if (Labels[Branchy] == -1)
                            Labels[Branchy] = LabelsNum++;
                        return "(IF $1 == 0, GO TO " + CurrBlock.BlockNum + "-" + CurrBlock.GraphAssociatedToBlock[Graph.Top.Start] + "-" + Labels[Branchy] + ")";
                    }
                    return "(IF $1 == 0)";
                case 0xC:
                    if (Zone.CycleIn != null && Zone.CycleIn.RealestStart.Start == Utilities.ToInt2Ints(Y, Z))
                        return "";
                    int Branch = Utilities.ToInt2Ints(Y, Z);
                    if (Labels[Branch] == -1)
                        Labels[Branch] = LabelsNum++;
                    return "[GO TO " + CurrBlock.BlockNum + "-" + CurrBlock.GraphAssociatedToBlock[Graph.Top.Start] + "-" + Labels[Branch] + "]";
                default:
                    return TranslateToText(Block0, CurrBlock);
            }
        }

        public String InsertPreviousCommand(List<int> DataList, String t, List<string> tmp, bool Graph)
        {
            if (this.NumParam != 0)
            {
                for (int i = 1; i <= this.NumParam && DataList.Count - i >= 0; i++)
                {
                    int tempData = DataList[DataList.Count - i];
                    if (tempData == SingleCommand.ImpossibleData)
                    {
                        t = t.Replace("^$" + i, tmp[tmp.Count - i]);
                        t = t.Replace("$" + i, tmp[tmp.Count - i]);
                    }
                    else
                    {
                        if ((tempData <= -1) && this.CommandType == 4 && (this.Y == 0x32 || this.Y == 0x33))
                        {
                            if (tempData == -1)
                                t = t.Replace("^$" + i, "SPEAKER");
                            else if (tempData == -2)
                                t = t.Replace("^$" + i, "TEAM LEADER");
                            else if (tempData == -3)
                                t = t.Replace("^$" + i, "SPEAKER, NO GREY BAR");
                            else if (tempData < 0)
                                t = t.Replace("^$" + i, "???");
                            else
                                t = t.Replace("^$" + i, "CHARACTER " + tempData);
                        }
                        if (this.CommandType == 4 && this.Y == 0xA3)
                            t = tempData == 0 ? t.Replace("$" + i, "DUSTER") : tempData == 2 ? t.Replace("$" + i, "KUMATORA") : t;
                        if (this.CommandType == 4 && this.Y == 3)
                            t = t.Replace("$" + i, (tempData + 5).ToString());
                        t = t.Replace("$" + i, tempData.ToString());
                    }
                    if(Graph)
                        t = t.Replace(Environment.NewLine, "");
                }
                if (!(this.CommandType == 0xE && this.X == 0x10))
                {
                    tmp.RemoveRange(Math.Max(tmp.Count - this.NumParam, 0), Math.Min(this.NumParam, tmp.Count));
                    DataList.RemoveRange(Math.Max(DataList.Count - this.NumParam, 0), Math.Min(this.NumParam, DataList.Count));
                }
            }
            return t;
        }

        public int CommandType
        {
            get
            {
                return commandType;
            }

            set
            {
                commandType = value;
            }
        }

        public int X
        {
            get
            {
                return x;
            }

            set
            {
                x = value;
            }
        }

        public int Y
        {
            get
            {
                return y;
            }

            set
            {
                y = value;
            }
        }

        public int Z
        {
            get
            {
                return z;
            }

            set
            {
                z = value;
            }
        }

        public string Command
        {
            get
            {
                return command;
            }

            set
            {
                command = value;
            }
        }

        public int NumParam
        {
            get
            {
                return numParam;
            }

            set
            {
                numParam = value;
            }
        }

        public int Data
        {
            get
            {
                return data;
            }

            set
            {
                data = value;
            }
        }

        public int LabelsNum
        {
            get
            {
                return labelsNum;
            }

            set
            {
                labelsNum = value;
            }
        }

        public int PointedEntry { get => pointedEntry; set => pointedEntry = value; }
    }
    class ExtendedCodeParams
    {
        private static int[] extCodeParams;

        public static int[] ExtCodeParams
        {
            get
            {
                return extCodeParams;
            }

            set
            {
                extCodeParams = value;
            }
        }
    }
    class Block
    {
        private const int MAXBLOCKS = 0x4000;
        private int blockBeginning;
        private BlockStatus[] status;
        private int[] number;
        private int[] commandNum;
        private int[] graphAssociatedToBlock;
        private int[] blockAssociatedToGraph;
        private bool[] externalFunction;
        private bool[] mainCaller;
        private bool[] special;
        private bool[] fifteenth_bit;
        private int[] referencesTo;
        private int entryNum;
        private int blockNum;
        public Block(int blockBeginning) : this(0, blockBeginning)
        {
        }
        public Block(int num, int blockBeginning)
        {
            BlockBeginning = blockBeginning;
            EntryNum = 0;
            BlockNum = num;
            Status = new BlockStatus[MAXBLOCKS];
            Number = new int[MAXBLOCKS];
            CommandNum = new int[MAXBLOCKS];
            ReferencesTo = new int[MAXBLOCKS];
            GraphAssociatedToBlock = new int[MAXBLOCKS];
            BlockAssociatedToGraph = new int[MAXBLOCKS];
            ExternalFunction = new bool[MAXBLOCKS];
            Special = new bool[MAXBLOCKS];
            Fifteenth_bit = new bool[MAXBLOCKS];
            MainCaller = new bool[MAXBLOCKS];
            for (int i = 0; i < MAXBLOCKS; i++)
            {
                Status[i] = BlockStatus.end;
                GraphAssociatedToBlock[i] = -1;
            }
        }
        public void UpdateToDiscovered(int blockCommand)
        {
            Status[blockCommand] = BlockStatus.discovered;
            Number[blockCommand] = EntryNum;
            ReferencesTo[blockCommand]++;
            CommandNum[EntryNum] = blockCommand;
            MainCaller[EntryNum] = false;
            Special[EntryNum] = false;
            Fifteenth_bit[EntryNum] = false;
            EntryNum++;
        }
        public void UpdateToDiscovered(int blockCommand, bool main)
        {
            bool tmp = false, tmp15 = false;
            if (blockCommand < 0)
            {
                blockCommand = blockCommand & 0x7FFF;
                tmp = true;
            }
            if ((blockCommand & 0x4000) != 0)
            {
                blockCommand = blockCommand & 0x3FFF;
                tmp15 = true;
            }
            UpdateToDiscovered(blockCommand);
            MainCaller[EntryNum - 1] = main;
            Special[EntryNum - 1] = tmp;
            Fifteenth_bit[EntryNum - 1] = tmp15;
        }

        public void CheckBranch(int Branch, int Subptr, byte[] Data)
        {
            if (this.Status[Branch] != BlockStatus.start && this.Status[Branch] != BlockStatus.discovered)
            {
                if (this.Status[Branch] == BlockStatus.used || (Branch != 0 && (this.Status[Branch - 1] == BlockStatus.used || (this.Status[Branch - 1] == BlockStatus.start && (Data[Subptr - 4] != 0xC && Data[Subptr - 4] != 0x8 && Data[Subptr - 4] != 0x6 && Data[Subptr - 4] != 0x9)))))
                    this.ReferencesTo[Branch]++;
                this.UpdateToDiscovered(Branch);
            }
            else
                this.ReferencesTo[Branch]++;
        }

        public int[] ReferencesTo
        {
            get
            {
                return referencesTo;
            }

            set
            {
                referencesTo = value;
            }
        }

        internal BlockStatus[] Status
        {
            get
            {
                return status;
            }

            set
            {
                status = value;
            }
        }

        public int[] Number
        {
            get
            {
                return number;
            }

            set
            {
                number = value;
            }
        }

        public int[] CommandNum
        {
            get
            {
                return commandNum;
            }

            set
            {
                commandNum = value;
            }
        }

        public int EntryNum
        {
            get
            {
                return entryNum;
            }

            set
            {
                entryNum = value;
            }
        }

        public int BlockNum
        {
            get
            {
                return blockNum;
            }

            set
            {
                blockNum = value;
            }
        }

        public bool[] ExternalFunction
        {
            get
            {
                return externalFunction;
            }

            set
            {
                externalFunction = value;
            }
        }

        public int[] GraphAssociatedToBlock
        {
            get
            {
                return graphAssociatedToBlock;
            }

            set
            {
                graphAssociatedToBlock = value;
            }
        }

        public int[] BlockAssociatedToGraph
        {
            get
            {
                return blockAssociatedToGraph;
            }

            set
            {
                blockAssociatedToGraph = value;
            }
        }

        public bool[] MainCaller { get => mainCaller; set => mainCaller = value; }
        public bool[] Special { get => special; set => special = value; }
        public bool[] Fifteenth_bit { get => fifteenth_bit; set => fifteenth_bit = value; }
        public int BlockBeginning { get => blockBeginning; set => blockBeginning = value; }
    }
    class BlockNode
    {
        private BlockNode father, lChild, rChild, rReference, lReference, topOfSubGraph;
        private List<BlockNode> otherFathers;
        private int start, end, insideReferences, depth, indexInLinearizedGraph;
        private Else elseIn;
        private List<int> openParenthesisAt, closeParenthesisAt;
        private Cycle cycleIn;
        private List<BlockNode> openParenthesisAtConnectedTo, closeParenthesisAtConnectedTo, goToBlock;
        private bool lOtherGraph, rOtherGraph;
        private BranchType branch;

        public BlockNode(int start) : this(start, -1, 0, null, 0, null)
        {
        }

        public BlockNode(int start, int depth, BlockNode TopSub) : this(start, -1, 0, null, depth, TopSub)
        {
        }

        public BlockNode(int start, BlockNode father, int depth, BlockNode TopSub) : this(start, -1, 0, father, depth, TopSub)
        {
        }

        public BlockNode(int start, int end, BlockNode father, int depth, BlockNode TopSub) : this(start, end, 0, father, depth, TopSub)
        {
        }

        public BlockNode(int start, int end, int insideReferences, BlockNode father, int depth, BlockNode TopSub)
        {
            Father = father;
            Start = start;
            End = end;
            InsideReferences = insideReferences;
            Depth = depth;
            IndexInLinearizedGraph = 0;
            RReference = null;
            LReference = null;
            CycleIn = null;
            ElseIn = null;
            OpenParenthesisAt = new List<int>();
            CloseParenthesisAt = new List<int>();
            OpenParenthesisAtConnectedTo = new List<BlockNode>();
            GoToBlock = new List<BlockNode>();
            CloseParenthesisAtConnectedTo = new List<BlockNode>();
            TopOfSubGraph = TopSub;
            LChild = null;
            RChild = null;
            LOtherGraph = false;
            ROtherGraph = false;
            Branch = BranchType.NaB;
            OtherFathers = new List<BlockNode>();
        }

        public BlockNode MakeRChild(int blockCommand, int NextDeepness)
        {
            this.End = blockCommand - 1;
            this.RChild = new BlockNode(blockCommand, this, NextDeepness, this.TopOfSubGraph);
            return this.RChild;
        }

        public BlockNode MakeLChild(int Branch, int NextDeepness)
        {
            this.LChild = new BlockNode(Branch, this, NextDeepness, null);
            this.LChild.TopOfSubGraph = this.LChild;
            return this.LChild;
        }

        public void UpdateOpenParenthesis(BlockNode Destination, int Value)
        {
            this.OpenParenthesisAt.Add(Value);
            this.OpenParenthesisAtConnectedTo.Add(Destination);
        }

        public void UpdateCloseParenthesis(BlockNode Source, int Value)
        {
            this.CloseParenthesisAt.Add(Value);
            this.CloseParenthesisAtConnectedTo.Add(Source);
        }

        public int End
        {
            get
            {
                return end;
            }

            set
            {
                end = value;
            }
        }

        public int InsideReferences
        {
            get
            {
                return insideReferences;
            }

            set
            {
                insideReferences = value;
            }
        }

        public int Start
        {
            get
            {
                return start;
            }

            set
            {
                start = value;
            }
        }

        internal BlockNode Father
        {
            get
            {
                return father;
            }

            set
            {
                father = value;
            }
        }

        internal BlockNode LChild
        {
            get
            {
                return lChild;
            }

            set
            {
                lChild = value;
            }
        }

        internal BlockNode RChild
        {
            get
            {
                return rChild;
            }

            set
            {
                rChild = value;
            }
        }

        internal BranchType Branch
        {
            get
            {
                return branch;
            }

            set
            {
                branch = value;
            }
        }

        public int Depth
        {
            get
            {
                return depth;
            }

            set
            {
                depth = value;
            }
        }

        internal BlockNode RReference
        {
            get
            {
                return rReference;
            }

            set
            {
                rReference = value;
            }
        }

        internal BlockNode LReference
        {
            get
            {
                return lReference;
            }

            set
            {
                lReference = value;
            }
        }

        internal BlockNode TopOfSubGraph
        {
            get
            {
                return topOfSubGraph;
            }

            set
            {
                topOfSubGraph = value;
            }
        }

        public bool LOtherGraph
        {
            get
            {
                return lOtherGraph;
            }

            set
            {
                lOtherGraph = value;
            }
        }

        public bool ROtherGraph
        {
            get
            {
                return rOtherGraph;
            }

            set
            {
                rOtherGraph = value;
            }
        }

        public List<int> OpenParenthesisAt
        {
            get
            {
                return openParenthesisAt;
            }

            set
            {
                openParenthesisAt = value;
            }
        }

        public List<int> CloseParenthesisAt
        {
            get
            {
                return closeParenthesisAt;
            }

            set
            {
                closeParenthesisAt = value;
            }
        }

        internal List<BlockNode> OtherFathers
        {
            get
            {
                return otherFathers;
            }

            set
            {
                otherFathers = value;
            }
        }

        internal List<BlockNode> OpenParenthesisAtConnectedTo
        {
            get
            {
                return openParenthesisAtConnectedTo;
            }

            set
            {
                openParenthesisAtConnectedTo = value;
            }
        }

        internal List<BlockNode> CloseParenthesisAtConnectedTo
        {
            get
            {
                return closeParenthesisAtConnectedTo;
            }

            set
            {
                closeParenthesisAtConnectedTo = value;
            }
        }

        public int IndexInLinearizedGraph
        {
            get
            {
                return indexInLinearizedGraph;
            }

            set
            {
                indexInLinearizedGraph = value;
            }
        }

        internal List<BlockNode> GoToBlock
        {
            get
            {
                return goToBlock;
            }

            set
            {
                goToBlock = value;
            }
        }

        internal Cycle CycleIn
        {
            get
            {
                return cycleIn;
            }

            set
            {
                cycleIn = value;
            }
        }

        internal Else ElseIn
        {
            get
            {
                return elseIn;
            }

            set
            {
                elseIn = value;
            }
        }
    }
    class FullGraph
    {
        private BlockNode top;
        private List<BlockNode> linearizedGraph;
        private List<BlockNode> bottoms;

        public FullGraph(BlockNode top)
        {
            Top = top;
            LinearizedGraph = new List<BlockNode>();
            Bottoms = new List<BlockNode>();
        }

        public void GetParenthesis()
        {
            MakeLinearizedGraph(Top);
            foreach (BlockNode Bottom in bottoms)
            {
                BlockNode Destination = Bottom;
                //UpdateParenthesisTopBottom(Destination, Bottom.TopOfSubGraph);
                while (Destination != null && Destination.OtherFathers.Count() == 0)
                {
                    Destination = Destination.Father;
                }
                if (Destination != null)
                {
                    for (int i = 0; i < Destination.OtherFathers.Count(); i++)
                    {
                        BlockNode Objective = Destination;
                        BlockNode Source = Destination.OtherFathers[i];
                        BlockNode Common = Source;
                        while (Common != null && Common.TopOfSubGraph != Destination.TopOfSubGraph)
                            Common = Common.TopOfSubGraph.Father;
                        if(Common != null)
                        {
                            if (Common.Depth == Destination.Depth)
                            {
                                if (Common.IndexInLinearizedGraph >= Destination.IndexInLinearizedGraph && Common.CycleIn == Destination.CycleIn)
                                {
                                    bool DoWhile = true;
                                    if (Source.RReference != null)
                                    {
                                        while (Destination.LChild == null && Destination.LReference == null)
                                            Destination = Destination.RChild;
                                        DoWhile = false;
                                        if (Destination.RChild.IndexInLinearizedGraph > Source.IndexInLinearizedGraph && Destination.LChild != null)
                                            Destination = Destination.LChild;
                                        else
                                            Destination = Destination.RChild;
                                        UpdateParenthesisIfElseDoWhile(Common, Destination);
                                        if(Common.RChild != null)
                                            new Cycle(Destination.Father, Destination, Objective, Common, Common.RChild, LinearizedGraph, DoWhile);
                                        else
                                            new Cycle(Destination.Father, Destination, Objective, Common, Common.RReference, LinearizedGraph, DoWhile);
                                    }
                                    else
                                    {
                                    }
                                }
                            }
                        }
                    }
                }
            }

            foreach (BlockNode Bottom in bottoms)
            {
                BlockNode Destination = Bottom;
                UpdateParenthesisTopBottom(Destination, Bottom.TopOfSubGraph);
                while (Destination != null && Destination.OtherFathers.Count() == 0)
                {
                    Destination = Destination.Father;
                }
                if (Destination != null)
                {
                    for (int i = 0; i < Destination.OtherFathers.Count(); i++)
                    {
                        BlockNode Source = Destination.OtherFathers[i];
                        BlockNode Common = Source;
                        while (Common != null && Common.TopOfSubGraph != Destination.TopOfSubGraph)
                        {
                            Common = Common.TopOfSubGraph.Father;
                        }
                        if (Common == null)
                            Source.GoToBlock.Add(Destination);
                        else
                        {
                            if (Common.Depth == Destination.Father.Depth)
                            {
                                if (Common.CycleIn != Destination.Father.CycleIn)
                                {
                                    Source.GoToBlock.Add(Destination);
                                }
                                else if (Common.IndexInLinearizedGraph < Destination.IndexInLinearizedGraph)
                                {
                                    UpdateParenthesisIfElseDoWhile(Destination.Father, Common.RChild);
                                    if (Source != Common)
                                        new Else(Common.RChild, Destination.Father, Destination, LinearizedGraph);
                                }
                            }
                            else
                            {
                                Source.GoToBlock.Add(Destination);
                            }
                        }
                    }
                }
            }
        }

        private void UpdateParenthesisIfElseDoWhile(BlockNode Destination, BlockNode Common)
        {
            UpdateDepthFrom(Destination, Common);
            if (Destination.RChild != null)
            {
                Common.UpdateOpenParenthesis(Destination.RChild, 0);
                Destination.RChild.UpdateCloseParenthesis(Common, 0);
            }
            else
            {
                Common.UpdateOpenParenthesis(Destination, 0);
                Destination.UpdateCloseParenthesis(Common, 4);
            }
        }

        private void UpdateParenthesisTopBottom(BlockNode Destination, BlockNode Common)
        {

            UpdateDepthFrom(Destination, Common);
            UpdateDepth(Common.LChild);
            Common.UpdateOpenParenthesis(Destination, 0);
            Destination.UpdateCloseParenthesis(Common, 4);
        }

        private void MakeLinearizedGraph(BlockNode TMP)
        {
            while (TMP != null)
            {
                LinearizedGraph.Add(TMP);
                TMP.IndexInLinearizedGraph = LinearizedGraph.Count - 1;
                TMP.Depth = 0;
                if (TMP.LChild != null)
                    MakeLinearizedGraph(TMP.LChild);
                TMP = TMP.RChild;
            }
        }

        private void UpdateDepth(BlockNode TMP)
        {
            while (TMP != null)
            {
                TMP.Depth++;
                if (TMP.LChild != null)
                    UpdateDepth(TMP.LChild);
                TMP = TMP.RChild;
            }
        }

        private void UpdateDepthFrom(BlockNode Destination, BlockNode Source)
        {
            if (Destination.RChild != null)
            {
                for (int i = Source.IndexInLinearizedGraph; i < Destination.RChild.IndexInLinearizedGraph; i++)
                {
                    LinearizedGraph[i].Depth++;
                }
            }
            else
            {
                for (int i = Source.IndexInLinearizedGraph; i <= Destination.IndexInLinearizedGraph ; i++)
                {
                    LinearizedGraph[i].Depth++;
                }
            }
            UpdateDepth(Destination.LChild);
        }

        internal BlockNode Top
        {
            get
            {
                return top;
            }

            set
            {
                top = value;
            }
        }

        internal List<BlockNode> Bottoms
        {
            get
            {
                return bottoms;
            }

            set
            {
                bottoms = value;
            }
        }

        internal List<BlockNode> LinearizedGraph
        {
            get
            {
                return linearizedGraph;
            }

            set
            {
                linearizedGraph = value;
            }
        }
    }
    class Program
    {
        static void PreProcessSingleEntry(byte[] Data, Block Block0, Block CurrBlock, int CurrEntryNum, int Block0Logic, int EntryLogic)
        {
            int pointer = (CurrBlock.CommandNum[CurrEntryNum] * 4) + EntryLogic;
            int blockCommand = CurrBlock.CommandNum[CurrEntryNum];
            if (CurrBlock.Status[blockCommand] == BlockStatus.start)
                return;
            if (CurrBlock.Status[blockCommand] == BlockStatus.used || CurrBlock.Status[blockCommand] == BlockStatus.end)
                CurrBlock.ReferencesTo[blockCommand]++;
            if (blockCommand != 0 && CurrBlock.Status[blockCommand - 1] != BlockStatus.start && CurrBlock.Status[blockCommand - 1] != BlockStatus.discovered)
                CurrBlock.Status[blockCommand - 1] = BlockStatus.end;
            CurrBlock.Status[blockCommand] = BlockStatus.start;
            while (Data[pointer] != 0x9 && Data[pointer] != 0x6 && Data[pointer] != 0x8 && Data[pointer] != 0xC && (blockCommand == CurrBlock.CommandNum[CurrEntryNum] || (CurrBlock.Status[blockCommand] != BlockStatus.start && CurrBlock.Status[blockCommand] != BlockStatus.discovered))) //Termination codes
            {
                if (blockCommand != CurrBlock.CommandNum[CurrEntryNum])
                    if (CurrBlock.Status[blockCommand] != BlockStatus.used)
                        CurrBlock.Status[blockCommand] = BlockStatus.used;
                pointer += 4;
                blockCommand++;
            }
            if (CurrBlock.Status[blockCommand] != BlockStatus.start && CurrBlock.Status[blockCommand] != BlockStatus.discovered)
                CurrBlock.Status[blockCommand] = BlockStatus.end;
            else
                CurrBlock.ReferencesTo[blockCommand]++;
            blockCommand = CurrBlock.CommandNum[CurrEntryNum];
            pointer = (CurrBlock.CommandNum[CurrEntryNum] * 4) + EntryLogic;
            while (Data[pointer] != 0x9 && Data[pointer] != 0x6 && Data[pointer] != 0x8 && (blockCommand == CurrBlock.CommandNum[CurrEntryNum] || (CurrBlock.Status[blockCommand] != BlockStatus.start && CurrBlock.Status[blockCommand] != BlockStatus.discovered))) //Termination codes
            {
                if (Data[pointer] == 0xC || Data[pointer] == 0xD || Data[pointer] == 0x7 || Data[pointer] == 0x5)
                {
                    int Branch = Utilities.ToInt2Bytes(Data, pointer + 2);
                    if (Data[pointer] == 0x5)
                    {
                        int Subptr = (4 * Branch) + Block0Logic;
                        Block0.CheckBranch(Branch, Subptr, Data);
                        Block0.ExternalFunction[Branch] = true;
                    }
                    else
                    {
                        int Subptr = (4 * Branch) + EntryLogic;
                        CurrBlock.CheckBranch(Branch, Subptr, Data);
                        if (Data[pointer] == 0x7)
                            CurrBlock.ExternalFunction[Branch] = true;
                        if (Data[pointer] == 0xC)
                        {
                            break;
                        }
                    }
                }
                blockCommand++;
                pointer += 4;
            }
        }

        static FullGraph GraphOfEntry(byte[] Data, Block CurrBlock, int CurrEntryNum, int EntryLogic)
        {
            int pointer = (CurrBlock.CommandNum[CurrEntryNum] * 4) + EntryLogic;
            int blockCommand = CurrBlock.CommandNum[CurrEntryNum];
            int startBlockCommand = blockCommand;
            BlockNode[] CorrespondingNode = new BlockNode[0x10000];
            FullGraph Graph = new FullGraph(new BlockNode(blockCommand));
            int NextDepth = 0;
            BlockNode ExecutionGraph = Graph.Top;
            ExecutionGraph.InsideReferences++;
            ExecutionGraph.TopOfSubGraph = ExecutionGraph;
            do
            {
                bool MakeRChild = false;
                while (Data[pointer] != 0x9 && Data[pointer] != 0x6 && Data[pointer] != 0x8 && CorrespondingNode[blockCommand] == null)
                {
                    //Create Tree
                    if ((CurrBlock.Status[blockCommand] == BlockStatus.start || MakeRChild) && ExecutionGraph.Start != blockCommand)
                    {
                        ExecutionGraph = ExecutionGraph.MakeRChild(blockCommand, NextDepth);
                        ExecutionGraph.InsideReferences++;
                        MakeRChild = false;
                    }
                    CorrespondingNode[blockCommand] = ExecutionGraph;
                    if (Data[pointer] == 0xD)
                    {
                        int Branch = Utilities.ToInt2Ints(Data[pointer + 2], Data[pointer + 3]);
                        if (ExecutionGraph.Start != blockCommand)
                        {
                            ExecutionGraph = ExecutionGraph.MakeRChild(blockCommand, NextDepth);
                            CorrespondingNode[blockCommand] = ExecutionGraph;
                        }
                        MakeRChild = true; // We need to make a new RChild to avoid issues with if/else/while-s
                        BlockNode TMP = ExecutionGraph;
                        while (TMP != null && CorrespondingNode[Branch] != null && TMP.TopOfSubGraph != CorrespondingNode[Branch].TopOfSubGraph)
                        {
                            TMP = TMP.TopOfSubGraph.Father;
                        }
                        if (CorrespondingNode[Branch] != null && TMP != null)
                        {
                            //We have a do while! We need to proceed accordingly
                            NextDepth--;
                            CorrespondingNode[Branch].InsideReferences++;
                            ExecutionGraph.LReference = CorrespondingNode[Branch];
                            CorrespondingNode[Branch].OtherFathers.Add(ExecutionGraph);
                        }
                        else
                        {
                            ExecutionGraph.MakeLChild(Branch, NextDepth);
                            ExecutionGraph.LChild.InsideReferences++;
                            NextDepth++;
                        }
                    }
                    if (Data[pointer] == 0xC)
                    {
                        int Branch = Utilities.ToInt2Ints(Data[pointer + 2], Data[pointer + 3]);
                        if (ExecutionGraph.Start != blockCommand)
                        {
                            ExecutionGraph = ExecutionGraph.MakeRChild(blockCommand, NextDepth);
                            CorrespondingNode[blockCommand] = ExecutionGraph;
                        }
                        if (CorrespondingNode[Branch] == null)
                        {
                            ExecutionGraph.End = blockCommand;
                            blockCommand = Branch - 1; //Follow the 0xC execution
                            pointer = (blockCommand * 4) + EntryLogic;
                            if (Data[pointer + 4] == 0x9 || Data[pointer + 4] == 0x6 || Data[pointer + 4] == 0x8)
                            {
                                ExecutionGraph.MakeRChild(blockCommand + 1, NextDepth);
                                ExecutionGraph = ExecutionGraph.RChild;
                                ExecutionGraph.InsideReferences++;
                            }
                        }
                        else
                        {
                            BlockNode TMPtmp = ExecutionGraph;
                            while (TMPtmp != null && TMPtmp.TopOfSubGraph != CorrespondingNode[Branch].TopOfSubGraph)
                                TMPtmp = TMPtmp.TopOfSubGraph.Father;
                            while (TMPtmp != null && TMPtmp.TopOfSubGraph == CorrespondingNode[Branch].TopOfSubGraph && TMPtmp != CorrespondingNode[Branch])
                                TMPtmp = TMPtmp.Father;
                            //Do we have a while?
                            if (TMPtmp != null && TMPtmp.TopOfSubGraph == CorrespondingNode[Branch].TopOfSubGraph) //If we have a while, de-tree-alize the branch forward
                            {
                                ExecutionGraph.End = blockCommand;
                                BlockNode TMP = CorrespondingNode[Branch];
                                TMP.OtherFathers.Add(ExecutionGraph);
                                TMP.InsideReferences++;
                                while (TMP.LChild == null && TMP.LReference == null)
                                    TMP = TMP.RChild;
                                if (TMP.LChild != null)
                                {
                                    ExecutionGraph.RReference = TMP.LChild;
                                    TMP.LChild.Father = ExecutionGraph;
                                    TMP.LChild.TopOfSubGraph = ExecutionGraph.TopOfSubGraph;
                                    TMP.LReference = ExecutionGraph.RReference;
                                    TMP.LChild = null;
                                    ExecutionGraph.RReference.OtherFathers.Add(TMP);
                                }
                                else
                                {
                                    ExecutionGraph.RReference = TMP.LReference;
                                }
                            }
                            else
                            {
                                ExecutionGraph.RReference = CorrespondingNode[Branch];
                                ExecutionGraph.RReference.InsideReferences++;
                            }
                            ExecutionGraph.RChild = ExecutionGraph.RReference;
                            blockCommand = ExecutionGraph.Start - 1;
                            pointer = (blockCommand * 4) + EntryLogic;
                        }
                    }
                    blockCommand++;
                    pointer += 4;
                }
                if (CorrespondingNode[blockCommand] == null) //New end
                {
                    if ((CurrBlock.Status[blockCommand] == BlockStatus.start || MakeRChild) && ExecutionGraph.Start != blockCommand)
                    {
                        ExecutionGraph = ExecutionGraph.MakeRChild(blockCommand, NextDepth);
                        ExecutionGraph.InsideReferences++;
                    }
                    ExecutionGraph.End = blockCommand;
                    CorrespondingNode[blockCommand] = ExecutionGraph;
                    Graph.Bottoms.Add(ExecutionGraph);
                }
                else if ((ExecutionGraph.Father != null) && ExecutionGraph.Father.LChild == ExecutionGraph && ExecutionGraph.Father.LChild.Start == CorrespondingNode[blockCommand].Start) //Reference the old endings
                {
                    ExecutionGraph.Father.LReference = CorrespondingNode[blockCommand];
                    CorrespondingNode[blockCommand].OtherFathers.Add(ExecutionGraph.Father);
                    if (ExecutionGraph != CorrespondingNode[blockCommand])
                    {
                        ExecutionGraph.Father.LReference.InsideReferences++;
                    }
                    ExecutionGraph.Father.LChild = null;
                }
                else
                {
                    CorrespondingNode[blockCommand].OtherFathers.Add(ExecutionGraph);
                    ExecutionGraph.RChild = null;
                }
                //Do something with the bottoms
                BlockNode prev = ExecutionGraph;
                while (ExecutionGraph != null && (ExecutionGraph.LChild == null || ExecutionGraph.LChild == prev))
                {
                    prev = ExecutionGraph;
                    ExecutionGraph = ExecutionGraph.Father;
                }
                if (ExecutionGraph != null)
                {
                    ExecutionGraph = ExecutionGraph.LChild;
                    NextDepth = ExecutionGraph.Depth;
                    blockCommand = ExecutionGraph.Start;
                    pointer = (blockCommand * 4) + EntryLogic;
                }
            } while (ExecutionGraph != null);

            /*for (int i = 0; i < CurrBlock.EntryNum; i++) //DEBUG CODE
            {
                int block = CurrBlock.CommandNum[i];
                if (CorrespondingNode[block] != null)
                {
                    int NumRef = CurrBlock.ReferencesTo[block];
                    if (CorrespondingNode[block].InsideReferences == NumRef || CorrespondingNode[block] == Graph.Top)
                    {
                    }
                    else if (CorrespondingNode[block].InsideReferences > NumRef)
                    {
                        i = i;
                        System.Console.WriteLine(CorrespondingNode[block].Start + " " + i);
                    }
                    else
                    {
                        if (CorrespondingNode[block].Father != null && CorrespondingNode[block].Father.RChild == CorrespondingNode[block])
                        {
                            CorrespondingNode[block].Father.ROtherGraph = true;
                            CorrespondingNode[block].Father.RChild = null;
                        }
                        else if (CorrespondingNode[block].Father != null)
                        {
                            CorrespondingNode[block].Father.LOtherGraph = true;
                            CorrespondingNode[block].Father.LChild = null;
                        }
                        CorrespondingNode[block].Father = null;
                        for (int j = 0; j < Graph.Bottoms.Count; j++)
                        {
                            BlockNode TMP = Graph.Bottoms[j];
                            while (TMP != null && TMP.TopOfSubGraph != Graph.Top.TopOfSubGraph && TMP.TopOfSubGraph != CorrespondingNode[block].TopOfSubGraph)
                                TMP = TMP.TopOfSubGraph.Father;
                            if (TMP != null && TMP.TopOfSubGraph == CorrespondingNode[block].TopOfSubGraph)
                            {
                                while (TMP.Father != null && TMP.TopOfSubGraph == CorrespondingNode[block].TopOfSubGraph)
                                {
                                    TMP = TMP.Father;
                                }
                                if (TMP.Father == null && TMP != Graph.Top)
                                {
                                    Graph.Bottoms.RemoveAt(j);
                                }
                            }
                        }
                    }
                }
            }*/

            return Graph;
        }

        static List<FullGraph> PreProcessEntries(byte[] Data, ref PointerCouple entry, ref PointerCouple block0, Block Block0, Block CurrBlock, bool Linear)
        {
            int entries = Utilities.ToInt2Bytes(Data, entry.Pointers);
            for (int i=0; i<entries; i++) //Separate the two, so there won't be repeated things in the future
            {
                CurrBlock.UpdateToDiscovered(Utilities.ToInt2Bytes(Data, entry.Pointers + 2 * (i + 1)), true);
            }
            for (int i = 0; i < CurrBlock.EntryNum; i++)
            {
                PreProcessSingleEntry(Data, Block0, CurrBlock, i, block0.Logic, entry.Logic);
            }
            int[] DifferentTopReferences = new int[CurrBlock.EntryNum];
            List<FullGraph> CurrBlockGraph = new List<FullGraph>();
            if (!Linear)
            {
                for (int i = 0; i < CurrBlock.EntryNum; i++)
                {
                    bool Valid = (i < entries) ? true : false;
                    if (Valid || CurrBlock.ExternalFunction[CurrBlock.CommandNum[i]])
                    {
                        CurrBlockGraph.Add(GraphOfEntry(Data, CurrBlock, i, entry.Logic));
                        CurrBlockGraph[CurrBlockGraph.Count - 1].GetParenthesis();
                        CurrBlock.GraphAssociatedToBlock[CurrBlock.CommandNum[i]] = CurrBlockGraph.Count - 1;
                        CurrBlock.BlockAssociatedToGraph[CurrBlockGraph.Count -1] = CurrBlock.CommandNum[i];
                    }
                }
            }
            return CurrBlockGraph;
        }

        static string[] ReadGameLogicEntries(byte[] Data, PointerCouple CurrentBlock, Block CurrBlock, Block Block0, List<FullGraph> Tree, bool Linear)
        {
            if (Linear)
                return ReadGameLogicEntries(Data, CurrentBlock, CurrBlock, Block0);
            return ReadGameLogicEntries(Data, CurrentBlock, CurrBlock, Block0, Tree);
        }

        static string[] ReadGameLogicEntries(byte[] Data, PointerCouple CurrentBlock, Block CurrBlock, Block Block0)
        {
            int NumElem = CurrBlock.EntryNum;
            List<String> ReadGameLogic = new List<string>();
            bool found = false;
            for (int i = 0; i < NumElem; i++)
            {
                if (CurrBlock.BlockNum == 0)
                {
                    if (!found || CurrBlock.MainCaller[i])
                    {
                        if (CurrBlock.MainCaller[i])
                            found = true;
                        /*
                        List<Arguments> argsStack = new List<Arguments>();
                        string basis = (CurrBlock.MainCaller[i] ? "C-" : "") + (CurrBlock.Special[i] ? "S-" : "") + (CurrBlock.Fifteenth_bit[i] ? "Fif-" : "") + CurrBlock.BlockNum.ToString() + "-" + i.ToString() + ": ";
                        do
                        {
                            if (argsStack.Count == 0)
                                ReadGameLogic.Add(basis + ReadGameLogicSingleEntry(Data, (CurrBlock.CommandNum[i] * 4) + CurrentBlock.Logic, CurrBlock.BlockNum, i, CurrBlock, Block0, argsStack));
                            else
                            {
                                Arguments tmpArg = argsStack[argsStack.Count - 1];
                                argsStack.RemoveAt(argsStack.Count - 1);
                                ReadGameLogic.Add(basis + ReadGameLogicSingleEntry(tmpArg, argsStack));
                            }
                        }
                        while (argsStack.Count > 0);*/
                        string basis = (CurrBlock.MainCaller[i] ? "C-" : "") + (CurrBlock.Special[i] ? "S-" : "") + (CurrBlock.Fifteenth_bit[i] ? "Fif-" : "") + CurrBlock.BlockNum.ToString() + "-" + i.ToString() + ": ";
                        ReadGameLogic.Add(basis + ReadGameLogicSingleEntry(Data, (CurrBlock.CommandNum[i] * 4) + CurrentBlock.Logic, CurrBlock.BlockNum, i, CurrBlock, Block0, null));
                    }
                    else
                    {
                        string basis = CurrBlock.BlockNum.ToString() + "-" + i.ToString() + ": ";
                        ReadGameLogic.Add(basis + ReadGameLogicSingleEntry(Data, (CurrBlock.CommandNum[i] * 4) + CurrentBlock.Logic, CurrBlock.BlockNum, i, CurrBlock, Block0, null));
                    }
                }
                else
                {
                    if (CurrBlock.MainCaller[i])
                    {
                        /*
                        List<Arguments> argsStack = new List<Arguments>();
                        string basis = (CurrBlock.MainCaller[i] ? "C-" : "") + (CurrBlock.Special[i] ? "S-" : "") + (CurrBlock.Fifteenth_bit[i] ? "Fif-" : "") + CurrBlock.BlockNum.ToString() + "-" + i.ToString() + ": ";
                        do
                        {
                            if (argsStack.Count == 0)
                                ReadGameLogic.Add(basis + ReadGameLogicSingleEntry(Data, (CurrBlock.CommandNum[i] * 4) + CurrentBlock.Logic, CurrBlock.BlockNum, i, CurrBlock, Block0, argsStack));
                            else
                            {
                                Arguments tmpArg = argsStack[argsStack.Count - 1];
                                argsStack.RemoveAt(argsStack.Count - 1);
                                ReadGameLogic.Add(basis + ReadGameLogicSingleEntry(tmpArg, argsStack));
                            }
                        }
                        while (argsStack.Count > 0);*/
                        string basis = (CurrBlock.MainCaller[i] ? "C-" : "") + (CurrBlock.Special[i] ? "S-" : "") + (CurrBlock.Fifteenth_bit[i] ? "Fif-" : "") + CurrBlock.BlockNum.ToString() + "-" + i.ToString() + ": ";
                        ReadGameLogic.Add(basis + ReadGameLogicSingleEntry(Data, (CurrBlock.CommandNum[i] * 4) + CurrentBlock.Logic, CurrBlock.BlockNum, i, CurrBlock, Block0, null));
                    }
                    else
                    {
                        string basis = CurrBlock.BlockNum.ToString() + "-" + i.ToString() + ": ";
                        ReadGameLogic.Add(basis + ReadGameLogicSingleEntry(Data, (CurrBlock.CommandNum[i] * 4) + CurrentBlock.Logic, CurrBlock.BlockNum, i, CurrBlock, Block0, null));
                    }
                }
            }
            return ReadGameLogic.ToArray();
        }

        static string[] ReadGameLogicEntries(byte[] Data, PointerCouple CurrentBlock, Block CurrBlock, Block Block0, List<FullGraph> Graph)
        {
            int NumElem = Graph.Count;
            string[] ReadGameLogic = new string[NumElem];
            for (int i = 0; i < NumElem; i++)
            {
                ReadGameLogic[i] = CurrBlock.BlockNum + "-" + i + ":" + Environment.NewLine;
                ReadGameLogic[i] += ReadGameLogicSingleEntry(Data, CurrentBlock.Logic, CurrBlock.BlockNum, i, CurrBlock, Block0, Graph[i].Top, Graph[i]);
            }
            return ReadGameLogic;
        }

        static string ReadGameLogicSingleEntry(byte[] Data, int start, int bank, int num, Block CurrBlock, Block Block0, List<Arguments> argsStack)
        {
            List<string> tmp = new List<string>();
            List<int> DataList = new List<int>();
            bool[] visitedBlock = new bool[CurrBlock.EntryNum];
            for (int i = 0; i < visitedBlock.Length; i++)
                visitedBlock[i] = false;
            int numblock = CurrBlock.CommandNum[num];
            List<int> returningStack = new List<int>();
            if (argsStack == null)
                returningStack = null;
            return ReadGameLogicSingleEntry(Data, start, bank, CurrBlock, Block0, tmp, DataList, numblock, visitedBlock, returningStack, argsStack);
        }

        static string ReadGameLogicSingleEntry(Arguments arguments, List<Arguments> argsStack)
        { return ReadGameLogicSingleEntry(arguments.Data, arguments.start, arguments.bank, arguments.CurrBlock, arguments.Block0, arguments.tmp, arguments.DataList, arguments.numblock, arguments.visitedBlock, arguments.returningStack, argsStack); }

        static string ReadGameLogicSingleEntry(byte[] Data, int start, int bank, Block CurrBlock, Block Block0, List<string>tmp, List<int> DataList, int numblock, bool[] visitedBlock, List<int> returningStack, List<Arguments> argsStack)
        {
            bool end = false;
            int tmpNumBlock;
            int tmpStart;
            do
            {
                if (CurrBlock.Status[numblock] == BlockStatus.start)
                    visitedBlock[CurrBlock.Number[numblock]] = true;
                SingleCommand c = SingleCommand.GetSingleCommand(Data, start, Block0, CurrBlock, DataList, tmp);
                if(argsStack != null)
                    if (c.CommandType == 0xD || c.CommandType == 0xC || c.CommandType == 8 || c.CommandType == 7)
                        tmp.Remove(tmp[tmp.Count - 1]);
                switch (c.CommandType)
                {
                    case 0x7:
                        if (!visitedBlock[CurrBlock.Number[CurrBlock.CommandNum[c.PointedEntry]]])
                        {
                            if (returningStack != null)
                            {
                                returningStack.Add(numblock);
                                numblock = CurrBlock.CommandNum[c.PointedEntry] - 1;
                                start = (numblock * 4) + CurrBlock.BlockBeginning;
                            }
                        }
                        break;
                    case 0x8:
                        if (returningStack != null)
                        {
                            numblock = returningStack[returningStack.Count - 1];
                            returningStack.RemoveAt(returningStack.Count - 1);
                            start = (numblock * 4) + CurrBlock.BlockBeginning;
                        }
                        else
                            end = true;
                        break;
                    case 0xD:
                        if (!visitedBlock[CurrBlock.Number[CurrBlock.CommandNum[c.PointedEntry]]])
                        {
                            if (argsStack != null)
                            {
                                tmpNumBlock = CurrBlock.CommandNum[c.PointedEntry];
                                visitedBlock[CurrBlock.Number[tmpNumBlock]] = true;
                                tmpStart = (tmpNumBlock * 4) + CurrBlock.BlockBeginning;
                                argsStack.Add(new Arguments(Data, tmpStart, bank, CurrBlock, Block0, tmp.ToList(), DataList.ToList(), tmpNumBlock, visitedBlock, returningStack.ToList()));
                            }
                        }
                        break;
                    case 5:
                        break;
                    case 6:
                        end = true;
                        break;
                    case 0xC:
                        if (argsStack != null)
                        {
                            if (visitedBlock[CurrBlock.Number[CurrBlock.CommandNum[c.PointedEntry]]])
                            {
                                end = true;
                                tmp.Add("[END]");
                            }
                            else
                            {
                                numblock = CurrBlock.CommandNum[c.PointedEntry] - 1;
                                start = (numblock * 4) + CurrBlock.BlockBeginning;
                            }
                        }
                        else
                            end = true;
                        break;
                    case 9:
                        end = true;
                        break;
                    default:
                        break;
                }
                numblock++;
                start += 4;
            }
            while (!end && (argsStack != null || CurrBlock.Status[numblock] != BlockStatus.start));
            string tmp2 = "";
            for (int i = 0; i < tmp.Count; i++)
                tmp2 += tmp[i];
            if (argsStack == null && !end && CurrBlock.Status[numblock] == BlockStatus.start)
                tmp2 += "[DO " + CurrBlock.BlockNum + "-" + CurrBlock.Number[numblock] + "]";
            return tmp2;
        }

        static string ReadGameLogicSingleEntry(byte[] Data, int Pointer, int bank, int num, Block CurrBlock, Block Block0, BlockNode Zone, FullGraph Graph)
        {
            List<string> tmp = new List<string>();
            List<int> DataList = new List<int>();
            int[] Labels = new int[0x10000];
            for (int i = 0; i < 0x10000; i++)
                Labels[i] = -1;
            int LabelsNum = 0;
            do
            {
                for (int i = 0; i < Zone.CloseParenthesisAt.Count; i++)
                {
                    if (Zone.CloseParenthesisAt[i] == 0)
                    {
                        tmp.Add("}" + Environment.NewLine);
                        break;
                    }
                }
                int numblock = Zone.Start;
                int start = (numblock * 4) + Pointer;
                if (Zone.OtherFathers.Count != 0)
                {
                    if(Labels[numblock] == -1)
                        Labels[numblock] = LabelsNum++;
                    tmp.Add(CurrBlock.BlockNum + "-" + num + "-" + Labels[numblock] + ":" + Environment.NewLine);
                }
                if (Zone.ElseIn != null && Zone.ElseIn.Start == Zone)
                {
                    tmp.Add("ELSE ");
                }
                if (Zone.LChild != null)
                {
                    if (!Zone.LChild.OpenParenthesisAt.Contains(0))
                    {
                        Zone.OpenParenthesisAt.Add(4);
                        BlockNode TMP = Zone;
                        while (TMP.RChild == null)
                            TMP = TMP.Father;
                        TMP.RChild.CloseParenthesisAt.Add(0);
                    }
                }
                for (int i = 0; i < Zone.OpenParenthesisAt.Count; i++)
                {
                    if (Zone.OpenParenthesisAt[i] == 0)
                    {
                        tmp.Add("{" + Environment.NewLine);
                        break;
                    }
                }
                while (numblock <= Zone.End)
                {
                    SingleCommand c = SingleCommand.GetSingleCommand(Data, start, Block0, CurrBlock, DataList, tmp, Zone, Graph, Labels, LabelsNum);
                    LabelsNum = c.LabelsNum;
                    numblock++;
                    start += 4;
                }
                for (int i = 0; i < Zone.CloseParenthesisAt.Count; i++)
                {
                    if (Zone.CloseParenthesisAt[i] == 4)
                    {
                        tmp.Add("}" + Environment.NewLine);
                        break;
                    }
                }
                for (int i = 0; i < Zone.OpenParenthesisAt.Count; i++)
                {
                    if (Zone.OpenParenthesisAt[i] == 4)
                    {
                        tmp.Add("{" + Environment.NewLine);
                        break;
                    }
                }
                if (Zone.GoToBlock.Count != 0)
                {
                    if (Zone.CycleIn == null || (Zone.GoToBlock[0] != Zone.CycleIn.RealestStart))
                    {
                        if (Labels[Zone.GoToBlock[0].Start] == -1)
                            Labels[Zone.GoToBlock[0].Start] = LabelsNum++;
                        tmp.Add("[GOTO " + CurrBlock.BlockNum + "-" + num + "-" + Labels[Zone.GoToBlock[0].Start] + "]" + Environment.NewLine);
                    }
                }
                if (Zone.LChild != null)
                    Zone = Zone.LChild;
                else if (Zone.RChild != null)
                    Zone = Zone.RChild;
                else
                {
                    Zone = Zone.TopOfSubGraph.Father;
                    if (Zone != null && Zone != Graph.Top)
                        Zone = Zone.RChild;
                }
            }
            while (Zone != null && Zone != Graph.Top);
            string tmp2 = "";
            for (int i = 0; i < tmp.Count; i++)
                tmp2 += tmp[i];
            return tmp2;
        }

        static int Main(string[] args) //Decompile only for now
        {
            if (args.Count() < 1)
            {
                Console.WriteLine("Write the path to the ROM!\n");
                return -2;
            }
            byte[] Data = File.ReadAllBytes(args[0]);
            const int paramAddress = 0xD2D658;
            int[] extendedCodeParameters = new int[256];
            for (int i = 0; i < 256; i++)
            {
                extendedCodeParameters[i] = Utilities.ToInt4Bytes(Data, paramAddress + (i * 4));
            }
            ExtendedCodeParams.ExtCodeParams = extendedCodeParameters;
            const int baseaddress = 0x1198C10;
            int numentries = Utilities.ToInt4Bytes(Data, baseaddress)/2;
            PointerCouple[] entriespointers = new PointerCouple[numentries + 1];
            List<string>[] ConvertedEntries= new List<string>[numentries];
            entriespointers[0] = new PointerCouple();
            entriespointers[0].Pointers = Utilities.ToInt4Bytes(Data, baseaddress + 4) + baseaddress;
            entriespointers[0].Logic = Utilities.ToInt4Bytes(Data, baseaddress + 8) + baseaddress;
            Block Block0 = new Block(entriespointers[0].Logic);
            bool Linear = true;
            if (args.Count() >1 && args[1].Equals("g"))
                Linear = false;
            for (int i = 1; i < numentries + 1; i++)
            {
                entriespointers[i] = new PointerCouple();
                entriespointers[i].Pointers = Utilities.ToInt4Bytes(Data, baseaddress + 4 + (i * 8)) + baseaddress;
                entriespointers[i].Logic = Utilities.ToInt4Bytes(Data, baseaddress + 8 + (i * 8)) + baseaddress;
                Block CurrBlock;
                if (i != numentries)
                    CurrBlock = new Block(i, entriespointers[i].Logic);
                else
                {
                    CurrBlock = Block0;
                    i = 0;
                }
                ConvertedEntries[i] = new List<string>();
                ConvertedEntries[i].Add("[BLOCK " + i.ToString() + "] 0x" + entriespointers[i].Pointers.ToString("X"));
                List<FullGraph> Graph = null;
                if (entriespointers[i].Logic != baseaddress && entriespointers[i].Pointers != baseaddress)
                {
                    Graph = PreProcessEntries(Data, ref entriespointers[i], ref entriespointers[0], Block0, CurrBlock, Linear);
                    ConvertedEntries[i].AddRange(ReadGameLogicEntries(Data, entriespointers[i], CurrBlock, Block0, Graph, Linear));
                }
                if (i == 0)
                    i = numentries;
            }
                List<string> FinalProduct= new List<string>();
            if (Linear)
                FinalProduct.Add("L");
            else
                FinalProduct.Add("G");
            for (int i=0; i<numentries; i++)
            {
                FinalProduct.AddRange(ConvertedEntries[i]);
                FinalProduct.Add("");
            }
            File.WriteAllLines("GameLogic.txt", FinalProduct);
            return 0;
        }
        private static void Compile(string[] args)
        {

        }
    }
}
