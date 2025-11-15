using Emulator;
using System;
using System.Diagnostics;

namespace Pandowdy.Core;


/// <summary>
/// VA2M-specific Bus that owns the CPU connection and routes reads/writes to VA2MMemory.
/// Emits a VBlank event at ~60Hz based on wall-clock time.
/// </summary>
public sealed class VA2MBus(VA2MMemory ram, VA2MMemory auxram, VA2MMemory ROM) : IBus
{
    private int lastPc = 0;

    private Action?[] preCmdTable = new Action?[65536];



    private CPU? _cpu;
    private ulong _systemClock;
    public IMemory RAM => ram; // IMemory-typed view of VA2MMemory
    private IMemory AuxRAM => auxram;
    private IMemory Rom => ROM;
    public ulong SystemClockCounter => _systemClock;

    // VBlank at ~60Hz wall-clock
    private readonly long _vblankIntervalTicks = Stopwatch.Frequency / 60;
    private long _nextVblankTicks = Stopwatch.GetTimestamp();
    public event EventHandler? VBlank;

    public void Connect(CPU cpu)
    {
        SetupDebugTable();
        _cpu = cpu;
        _cpu.Connect(this);
    }

    void SetupDebugTable()
    {
        Action debugAction(UInt16 address, string label)
        {
            return () => Debug.WriteLine($"{label} at {address:X4}");
        }

        preCmdTable[0xD870] = debugAction(0xD870, "END");
        preCmdTable[0xD766] = debugAction(0xD766, "FOR");
        preCmdTable[0xDCF9] = debugAction(0xDCF9, "NEXT");
        preCmdTable[0xD995] = debugAction(0xD995, "DATA");
        preCmdTable[0xDBB2] = debugAction(0xDBB2, "INPUT");
        preCmdTable[0xF331] = debugAction(0xF331, "DEL");
        preCmdTable[0xDFD9] = debugAction(0xDFD9, "DIM");
        preCmdTable[0xDBE2] = debugAction(0xDBE2, "READ");
        preCmdTable[0xF390] = debugAction(0xF390, "GR");
        preCmdTable[0xF399] = debugAction(0xF399, "TEXT");
        preCmdTable[0xF1E5] = debugAction(0xF1E5, "PR#");
        preCmdTable[0xF1DE] = debugAction(0xF1DE, "IN#");
        preCmdTable[0xF1D5] = debugAction(0xF1D5, "CALL");
        preCmdTable[0xF225] = debugAction(0xF225, "PLOT");
        preCmdTable[0xF232] = debugAction(0xF232, "HLIN");
        preCmdTable[0xF241] = debugAction(0xF241, "VLIN");
        preCmdTable[0xF3D8] = debugAction(0xF3D8, "HGR2");
        preCmdTable[0xF3E2] = debugAction(0xF3E2, "HGR");
        preCmdTable[0xF6E9] = debugAction(0xF6E9, "HCOLOR");
        preCmdTable[0xF6FE] = debugAction(0xF6FE, "HPLOT");
        preCmdTable[0xF769] = debugAction(0xF769, "DRAW");
        preCmdTable[0xF76F] = debugAction(0xF76F, "XDRAW");
        preCmdTable[0xF7E7] = debugAction(0xF7E7, "HTAB");
        preCmdTable[0xFC58] = debugAction(0xFC58, "HOME");
        preCmdTable[0xF721] = debugAction(0xF721, "ROT=");
        preCmdTable[0xF727] = debugAction(0xF727, "SCALE=");
        preCmdTable[0xF775] = debugAction(0xF775, "SHLOAD");
        preCmdTable[0xF26D] = debugAction(0xF26D, "SETTRACE");
        preCmdTable[0xF26F] = debugAction(0xF26F, "TRACEOFF");
        preCmdTable[0xF273] = debugAction(0xF273, "SETNORM");
        preCmdTable[0xF277] = debugAction(0xF277, "INVERSE");
        preCmdTable[0xF280] = debugAction(0xF280, "FLASH");
        preCmdTable[0xF24F] = debugAction(address: 0xF24F, "COLOR");
        //POP/RETURN
        preCmdTable[0xD96B] = debugAction(0xD96B, "RETURN (POP)");
        preCmdTable[0xF256] = debugAction(0xF256, "VTAB");
        preCmdTable[0xF286] = debugAction(0xF286, "HIMEMSET");
        preCmdTable[0xF2A6] = debugAction(0xF2A6, "LOMEMSET");
        preCmdTable[0xF2CB] = debugAction(0xF2CB, "ONERR");
        preCmdTable[0xF318] = debugAction(0xF318, "RESUME");
        preCmdTable[0xF3BC] = debugAction(0xF3BC, "RECALL");
        preCmdTable[0xF39F] = debugAction(0xF39F, "STORE");
        preCmdTable[0xF262] = debugAction(0xF262, "SPEED");
        preCmdTable[0xDA46] = debugAction(0xDA46, "LET");
        preCmdTable[0xD93E] = debugAction(0xD93E, "GOTO");
        preCmdTable[0xD912] = debugAction(0xD912, "RUN");
        preCmdTable[0xD9C9] = debugAction(0xD9C9, "IF");
        preCmdTable[0xD849] = debugAction(0xD849, "RESTORE");
        preCmdTable[0x03F5] = debugAction(0x03F5, "& VECTOR");
        preCmdTable[0xD921] = debugAction(0xD921, "GOSUB");
        //POP/RETURN defined above
        preCmdTable[0xD9DC] = debugAction(0xD9DC, "REM");
        preCmdTable[0xD86E] = debugAction(0xD86E, "STOP");
        preCmdTable[0xD9EC] = debugAction(0xD9EC, "ONGOTO");
        preCmdTable[0xE784] = debugAction(0xE784, "WAIT");
        preCmdTable[0xD8B0] = debugAction(0xD8B0, "SAVE");
        preCmdTable[0xD8C9] = debugAction(0xD8C9, "LOAD");
        preCmdTable[0xE313] = debugAction(0xE313, "DEF");
        preCmdTable[0xE77B] = debugAction(0xE77B, "POKE");
        preCmdTable[0xDAD5] = debugAction(0xDAD5, "PRINT");
        preCmdTable[0xD896] = debugAction(0xD896, "CONT");
        preCmdTable[0xD6A5] = debugAction(0xD6A5, "LIST");
        preCmdTable[0xD66A] = debugAction(0xD66A, "CLEAR");
        preCmdTable[0xDBA0] = debugAction(0xDBA0, "GET");
        preCmdTable[0xD649] = debugAction(0xD649, "NEW");

        // Functions
        preCmdTable[0xEB90] = debugAction(0xEB90, "SGN");
        preCmdTable[0xEC23] = debugAction(0xEC23, "INT");
        preCmdTable[0xEBAF] = debugAction(0xEBAF, "ABS");
        preCmdTable[0x000A] = debugAction(0x000A, "USRVEC");
        preCmdTable[0xE2DE] = debugAction(0xE2DE, "FRE");
        preCmdTable[0xDEF9] = debugAction(0xDEF9, "SCRN("); // A.S. table points to ERROR mistakenly
        preCmdTable[0xDFCD] = debugAction(0xDFCD, "PDL");
        preCmdTable[0xE2FF] = debugAction(0xE2FF, "POS");
        preCmdTable[0xEE8D] = debugAction(0xEE8D, "SQR");
        preCmdTable[0xEFAE] = debugAction(0xEFAE, "RND");
        preCmdTable[0xE941] = debugAction(0xE941, "LOG");
        preCmdTable[0xEF09] = debugAction(0xEF09, "EXP");
        preCmdTable[0xEFEA] = debugAction(0xEFEA, "COS");
        preCmdTable[0xEFF1] = debugAction(0xEFF1, "SIN");
        preCmdTable[0xF03A] = debugAction(0xF03A, "TAN");
        preCmdTable[0xF09E] = debugAction(0xF09E, "ATN");
        preCmdTable[0xE764] = debugAction(0xE764, "PEEK");
        preCmdTable[0xE6D6] = debugAction(0xE6D6, "LEN");
        preCmdTable[0xE3C5] = debugAction(0xE3C5, "STR$");
        preCmdTable[0xE707] = debugAction(0xE707, "VAL");
        preCmdTable[0xE6E5] = debugAction(0xE6E5, "ASC");
        preCmdTable[0xE646] = debugAction(0xE646, "CHR$");
        preCmdTable[0xE65A] = debugAction(0xE65A, "LEFT$");
        preCmdTable[0xE686] = debugAction(0xE686, "RIGHT$");
        preCmdTable[0xE691] = debugAction(0xE691, "MID$");

        // MATH OPERATORS
        preCmdTable[0xE7C1] = debugAction(0xE7C1, "FADDT"); // +  (Prec 0x79)
        preCmdTable[0xE7AA] = debugAction(0xE7AA, "FSUBT"); // -  (Prec 0x79)
        preCmdTable[0xE982] = debugAction(0xE982, "FMULTT"); // *  (Prec 0x7B)
        preCmdTable[0xEA69] = debugAction(0xEA69, "FDIVT"); // /  (Prec 0x7B)
        preCmdTable[0xEE97] = debugAction(0xEE97, "FPWRT"); // ^  (Prec 0x7D)
        preCmdTable[0xDF55] = debugAction(0xDF55, "AND");  // AND (Prec 0x50)
        preCmdTable[0xDF4F] = debugAction(0xDF4F, "OR");   // OR (Prec 0x46)
        preCmdTable[0xEED0] = debugAction(0xEED0, "NEGOP"); // NEGATION (Prec 0x7F)
        preCmdTable[0xDE98] = debugAction(0xDE98, "NOTFAC"); // = (Prec 0x7f)
        preCmdTable[0xDF65] = debugAction(0xDF65, "RELOPS"); // RELOPS (Prec 0x64)



        preCmdTable[0xD828] = debugAction(0xD828, "EXEC_STMT");
        preCmdTable[0xDD7B] = debugAction(0xDD7B, "FRMEVL");
        preCmdTable[0xD412] = debugAction(0xD412, "ERROR");


        preCmdTable[0xD43c] = debugAction(0xD43C, "JUMPSTART");
        preCmdTable[0xE000] = debugAction(0xE000, "BASIC");
        preCmdTable[0xE003] = debugAction(0xE003, "BASIC2");
        preCmdTable[0xF128] = debugAction(0xF128, "COLDST");

        /*

        preCmdTable[0xD365] = debugAction(0xD365, "STKSRCH");
        preCmdTable[0xD393] = debugAction(0xD393, "BLTU");
        preCmdTable[0xD3D6] = debugAction(0xD3D6, "CHKMEM");
        preCmdTable[0xD3E3] = debugAction(0xD3E3, "REASON");
        preCmdTable[0xD410] = debugAction(0xD410, "MEMERROR");
        preCmdTable[0xD52C] = debugAction(0xD52C, "INLIN");
        preCmdTable[0xD52E] = debugAction(0xD52E, "INLIN+2");
        preCmdTable[0xD539] = debugAction(0xD539, "GDBUFS");
        preCmdTable[0xD553] = debugAction(0xD553, "INCHR");
        preCmdTable[0xD559] = debugAction(0xD559, "RUN1");
        preCmdTable[0xD56C] = debugAction(0xD56C, "RUN+");
        preCmdTable[0xD61A] = debugAction(0xD61A, "FNDLIN");
        preCmdTable[0xD64B] = debugAction(0xD64B, "SCRTCH");
        preCmdTable[0xD66C] = debugAction(0xD66C, "CLEARC");
        preCmdTable[0xD683] = debugAction(0xD683, "STKINI");
        preCmdTable[0xD697] = debugAction(0xD697, "STaddress: XTPT");
        preCmdTable[0xD6DA] = debugAction(0xD6DA, "LIST1LIN");
        preCmdTable[0xD7D2] = debugAction(0xD7D2, "NEWSTT");
        preCmdTable[0xD805] = debugAction(0xD805, "TRACE_");
        preCmdTable[0xD858] = debugAction(0xD858, "ISCNTC");

        preCmdTable[0xD8F0] = debugAction(0xD8F0, "VARTIO");
        preCmdTable[0xD901] = debugAction(0xD901, "PROGIO");



        preCmdTable[0xD941] = debugAction(0xD941, "GOTO<");
        preCmdTable[0xD979] = debugAction(0xD979, "RETURN w/o GOSUB");
        preCmdTable[0xD97C] = debugAction(0xD97C, "UNDEF'D STMT PRT");
        preCmdTable[0xD998] = debugAction(0xD998, "ADDON");
        preCmdTable[0xD9A3] = debugAction(0xD9A3, "DATAN");
        preCmdTable[0xD9A6] = debugAction(0xD9A6, "REMN");

        preCmdTable[0xDA0C] = debugAction(0xDA0C, "LINGET");
        preCmdTable[0xDAB7] = debugAction(0xDAB7, "COPY");
        preCmdTable[0xDAFB] = debugAction(0xDAFB, "CRDO");
        preCmdTable[0xDA3A] = debugAction(0xDA3A, "STROUT");
        preCmdTable[0xDB3D] = debugAction(0xDB3D, "STRPRT");
        preCmdTable[0xDB57] = debugAction(0xDB57, "OUTSP");
        preCmdTable[0xDB5A] = debugAction(0xDB5A, "OUTQST");
        //      preCmdTable[0xDB5C] = debugAction(0xDB5C, "OUTDO");
        preCmdTable[0xDD0B] = debugAction(0xDD0B, "NEXT w/o FOR PRT");
        preCmdTable[0xDD67] = debugAction(0xDD67, "FRMNUM");
        preCmdTable[0xDD6A] = debugAction(0xDD6A, "CHKNUM");
        preCmdTable[0xDD6C] = debugAction(0xDD6C, "CHKSTR");
        preCmdTable[0xDD6D] = debugAction(0xDD6D, "CHKVAL");
        preCmdTable[0xDD76] = debugAction(0xDD76, "TYPMISM");
        preCmdTable[0xDE47] = debugAction(0xDE47, "XORFPSIGN");
        preCmdTable[0xDE60] = debugAction(0xDE60, "FRM_ELEMENT");
        preCmdTable[0xDE81] = debugAction(0xDE81, "STRTXT");
        preCmdTable[0xDEB2] = debugAction(0xDEB2, "PARCHK");
        preCmdTable[0xDEB8] = debugAction(0xDEB8, "CHKCLS");
        preCmdTable[0xDEBB] = debugAction(0xDEBB, "CHKOPN");
        preCmdTable[0xDEBE] = debugAction(0xDEBE, "CHKCOM");
        preCmdTable[0xDEC0] = debugAction(0xDEC0, "SYNCHR");
        preCmdTable[0xDEC9] = debugAction(0xDEC9, "SYNERR");
        preCmdTable[0xDFE3] = debugAction(0xDFE3, "PTRGET");

        preCmdTable[0xE07D] = debugAction(0xE07D, "ISLETC");
        preCmdTable[0xE105] = debugAction(0xE105, "EVAL EXPR => INT");
        preCmdTable[0xE108] = debugAction(0xE108, "AYPOSINT");
        preCmdTable[0xE10C] = debugAction(0xE10C, "AYINT");
        preCmdTable[0xE196] = debugAction(0xE196, "SUB ERR");
        preCmdTable[0xE199] = debugAction(0xE199, "QTY ERR");
        preCmdTable[0xE2F2] = debugAction(0xE2F2, "GIVAYF");
        preCmdTable[0xE301] = debugAction(0xE301, "SNGFLT");
        preCmdTable[0xE306] = debugAction(0xE306, "ERRDIR");
        preCmdTable[0xE30E] = debugAction(0xE30E, "UNDEF ERR");
        preCmdTable[0xE3D5] = debugAction(0xE3D5, "STRINI");
        preCmdTable[0xE3DD] = debugAction(0xE3DD, "STRSPA");
        preCmdTable[0xE3E7] = debugAction(0xE3E7, "STRLIT");
        preCmdTable[0xE3ED] = debugAction(0xE3ED, "STRLT2");
        preCmdTable[0xE42A] = debugAction(0xE42A, "PUTNEW");
        preCmdTable[0xE430] = debugAction(0xE430, "FRM ERR");
        preCmdTable[0xE452] = debugAction(0xE452, "GETSPA");
        preCmdTable[0xE484] = debugAction(0xE484, "GARBAG");
        preCmdTable[0xE597] = debugAction(0xE597, "CAT");
        preCmdTable[0xE5D4] = debugAction(0xE5D4, "MOVINS");
        preCmdTable[0xE5E2] = debugAction(0xE5E2, "MOVSTR");
        preCmdTable[0xE5FD] = debugAction(0xE5FD, "FRESTR");
        preCmdTable[0xE600] = debugAction(0xE600, "FREFAC");
        preCmdTable[0xE604] = debugAction(0xE604, "FRETMP");
        preCmdTable[0xE635] = debugAction(0xE635, "FRETMS");

        preCmdTable[0xE6F5] = debugAction(0xE6F5, "GTBYTC");
        preCmdTable[0xE6F8] = debugAction(0xE6F8, "GETBYT");
        preCmdTable[0xE6FB] = debugAction(0xE6FB, "CONINT");
        preCmdTable[0xE746] = debugAction(0xE746, "GETNUM");
        preCmdTable[0xE74C] = debugAction(0xE74C, "COMBYTE");
        preCmdTable[0xE752] = debugAction(0xE752, "GETADR");
        preCmdTable[0xE7A0] = debugAction(0xE7A0, "FADDH");
        preCmdTable[0xE7A7] = debugAction(0xE7A7, "FSUB");
        preCmdTable[0xE7BE] = debugAction(0xE7BE, "FADD");
        preCmdTable[0xE7D5] = debugAction(0xE7D5, "OVFLW ERR");
        preCmdTable[0xE97F] = debugAction(0xE97F, "FMULT");
        preCmdTable[0xE9E3] = debugAction(0xE9E3, "CONUPK");
        preCmdTable[0xEA39] = debugAction(0xEA39, "MUL10");
        preCmdTable[0xEA55] = debugAction(0xEA55, "DIV10");
        preCmdTable[0xEA66] = debugAction(0xEA66, "FDIV");
        preCmdTable[0xEAE1] = debugAction(0xEAE1, "DIVZERO ERR");
        preCmdTable[0xEAF9] = debugAction(0xEAF9, "MOVFM");
        preCmdTable[0xEB1E] = debugAction(0xEB1E, "MOV2F");
        preCmdTable[0xEB21] = debugAction(0xEB21, "MOV1F");
        preCmdTable[0xEB23] = debugAction(0xEB23, "MOVML");
        preCmdTable[0xEB2B] = debugAction(0xEB2B, "MOVMF");
        preCmdTable[0xEB53] = debugAction(0xEB53, "MOVFA");
        preCmdTable[0xEB63] = debugAction(0xEB63, "MOVAF");
        preCmdTable[0xEB66] = debugAction(0xEB66, "MOVAF2");
        preCmdTable[0xEB72] = debugAction(0xEB72, "RNDB");
        preCmdTable[0xEB82] = debugAction(0xEB82, "SIGN");
        preCmdTable[0xEB93] = debugAction(0xEB93, "FLOAT");



        preCmdTable[0xEBB2] = debugAction(0xEBB2, "FCOMP");
        preCmdTable[0xEBF2] = debugAction(0xEBF2, "QINT");
        preCmdTable[0xEC40] = debugAction(0xEC40, "INITFACMANT");
        preCmdTable[0xEC4A] = debugAction(0xEC4A, "FIN");
        preCmdTable[0xED19] = debugAction(0xED19, "INPRT");
        preCmdTable[0xED24] = debugAction(0xED24, "LINPRT");
        preCmdTable[0xED2E] = debugAction(0xED23, "PRNTFAC");
        preCmdTable[0xED34] = debugAction(0xED34, "FOUT");
        preCmdTable[0xEEFB] = debugAction(0xEEFB, "LOGE(2)");






        preCmdTable[0xF1EC] = debugAction(0xF1EC, "PLOTFNS");
        preCmdTable[0xF2E9] = debugAction(0xF2E9, "HANDLERR");



        preCmdTable[0xF3F2] = debugAction(0xF3F2, "HCLR");
        preCmdTable[0xF411] = debugAction(0xF411, "HPOSN");
        preCmdTable[0xF457] = debugAction(0xF457, "HPLOT0");
        preCmdTable[0xF53A] = debugAction(0xF53A, "HLINE");
        preCmdTable[0xF5CB] = debugAction(0xF5CB, "HFIND");
        preCmdTable[0xF601] = debugAction(0xF601, "DRAW1");
        preCmdTable[0xF65D] = debugAction(0xF65D, "XDRAW1");
        preCmdTable[0xF6B9] = debugAction(0xF6B9, "HFNS");
        preCmdTable[0xF6EC] = debugAction(0xF6EC, "SETHCOL");
        preCmdTable[0xF6F6] = debugAction(0xF6F6, "COLRMASK");



        preCmdTable[0xF7D9] = debugAction(0xF7D9, "GETARYPT");



        */




    }

    public byte CpuRead(ushort address, bool readOnly = false)
    {
        if (address < 0xC000)
        {
            return RAM.Read(address);
        }
        else if (address < 0xC100)
        {
            if (address == 0xC010)
            {
                var keyval = auxram.Read(0xC000);
                auxram.Write(0xC000, (byte) (keyval & 0x7F)); // clear high bit on read of strobe
            }
            return RAM.Read(address);
        }
        else
        {
            return ROM.Read(address);
        }
    }

    public void CpuWrite(ushort address, byte data)
    {
        if (address >= 0xD000)
        {
            // ROM area is not writable
            return;
        }
        else if (address >= 0xC000)
        {
            if (address == 0xC010)
            {
                var keyval = auxram.Read(0xC000);
                ram.Write(0xC000, (byte) (keyval & 0x7F)); // clear high bit on read of strobe
                return;
            }
            ram.Write(address, data);
        }
        else
        {
            ram.Write(address, data);
        }
    }


    public void Clock()
    {
        // PC trace (preserved)
        var currPc = _cpu!.PC;
        if (lastPc != currPc)
        {
            if (preCmdTable[currPc] != null)
            {
                var lineNum = CpuRead(0x75) + (CpuRead(0x76) * 256);
                string ln = lineNum < 0xfA00 ? lineNum.ToString() : "IMM";

                Debug.Write($"{ln.PadRight(5)} ");
                var sp = _cpu!.SP;
                var spcs = 0xff - sp;
                if (spcs > 0)
                {
                    Debug.Write(new string(' ', spcs));
                }
                preCmdTable[currPc]();
            }

            #region LegacyTrace_DoNotRemove
            // The following debug trace code is intentionally left commented for
            // temporary instrumentation during AppleSoft tracing. Do not remove.
            //if (currPc ==0xD805)
            //{
            // var lineNum = CpuRead(0x75) + (CpuRead(0x76) *256);
            // if (lineNum <0xFF00)
            // {
            // Debug.WriteLine($"LineNum: {lineNum}");
            // }
            // else
            // {
            // Debug.WriteLine("LineNum: IMMEDIATE");
            // }
            //}
            //else if (currPc ==0xD766)
            //{
            // Debug.WriteLine(" FOR");
            //}
            //else if (currPc ==0xDCF9)
            //{
            // Debug.WriteLine(" NEXT");
            //}
            #endregion
        }
        lastPc = currPc;

        // Execute one CPU cycle
        _cpu!.Clock();
        _systemClock++;

        // Wall-clock driven VBlank: coalesce multiple due events so we don't drift
        long now = Stopwatch.GetTimestamp();
        if (now >= _nextVblankTicks)
        {
            do
            {
                _nextVblankTicks += _vblankIntervalTicks;
            } while (now >= _nextVblankTicks);
            VBlank?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Reset()
    {
        _cpu!.Reset();
        _systemClock = 0;
        _nextVblankTicks = Stopwatch.GetTimestamp() + _vblankIntervalTicks;
    }
}
