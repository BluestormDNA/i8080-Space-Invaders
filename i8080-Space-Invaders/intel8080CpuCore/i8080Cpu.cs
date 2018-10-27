using System;

namespace BlueStorm.intel8080CpuCore {
    class Cpu {

        private byte[] memory;
        private i8080IObus iobus;
        private ushort PC;
        private ushort SP;
        private bool interruptPin;

        public int _2Mhz = 2000000;
        public int cycles { get; set; }
        private int[] cyclesValue = {
                4, 10, 7, 5, 5, 5, 7, 4, 4, 10, 7, 5, 5, 5, 7, 4, //0x00..0x0f
	            4, 10, 7, 5, 5, 5, 7, 4, 4, 10, 7, 5, 5, 5, 7, 4,
                4, 10, 16, 5, 5, 5, 7, 4, 4, 10, 16, 5, 5, 5, 7, 4,
                4, 10, 13, 5, 10, 10, 10, 4, 4, 10, 13, 5, 5, 5, 7, 4,

                5, 5, 5, 5, 5, 5, 7, 5, 5, 5, 5, 5, 5, 5, 7, 5, //0x40..0x4f
	            5, 5, 5, 5, 5, 5, 7, 5, 5, 5, 5, 5, 5, 5, 7, 5,
                5, 5, 5, 5, 5, 5, 7, 5, 5, 5, 5, 5, 5, 5, 7, 5,
                7, 7, 7, 7, 7, 7, 7, 7, 5, 5, 5, 5, 5, 5, 7, 5,

                4, 4, 4, 4, 4, 4, 7, 4, 4, 4, 4, 4, 4, 4, 7, 4, //0x80..8x4f
	            4, 4, 4, 4, 4, 4, 7, 4, 4, 4, 4, 4, 4, 4, 7, 4,
                4, 4, 4, 4, 4, 4, 7, 4, 4, 4, 4, 4, 4, 4, 7, 4,
                4, 4, 4, 4, 4, 4, 7, 4, 4, 4, 4, 4, 4, 4, 7, 4,

                11, 10, 10, 10, 17, 11, 7, 11, 11, 10, 10, 10, 10, 17, 7, 11, //0xc0..0xcf
	            11, 10, 10, 10, 17, 11, 7, 11, 11, 10, 10, 10, 10, 17, 7, 11,
                11, 10, 10, 18, 17, 11, 7, 11, 11, 5, 10, 5, 17, 17, 7, 11,
                11, 10, 10, 4, 17, 11, 7, 11, 11, 5, 10, 4, 17, 17, 7, 11,
                };

        private byte A, B, C, D, E, F, H, L;

        private ushort AF { get { return combineRegisters(A, F); } set { A = (byte)(value >> 8); F = (byte)value; } }
        private ushort BC { get { return combineRegisters(B, C); } set { B = (byte)(value >> 8); C = (byte)value; } }
        private ushort DE { get { return combineRegisters(D, E); } set { D = (byte)(value >> 8); E = (byte)value; } }
        private ushort HL { get { return combineRegisters(H, L); } set { H = (byte)(value >> 8); L = (byte)value; } }

        private bool FlagS { get { return (F & 0x80) != 0; } set { F = value ? (byte)(F | 0x80) : (byte)(F & ~0x80); } }
        private bool FlagZ { get { return (F & 0x40) != 0; } set { F = value ? (byte)(F | 0x40) : (byte)(F & ~0x40); } }
        //private bool Flag0x20_0 { get { return (F & 0x20) != 0; } set { F = value ? (byte)(F | 0x20) : (byte)(F & ~0x20); } }
        private bool FlagAC { get { return (F & 0x10) != 0; } set { F = value ? (byte)(F | 0x10) : (byte)(F & ~0x10); } }
        //private bool Flag0x8_0 { get { return (F & 0x8) != 0; } set { F = value ? (byte)(F | 0x8) : (byte)(F & ~0x8); } }
        private bool FlagP { get { return (F & 0x4) != 0; } set { F = value ? (byte)(F | 0x4) : (byte)(F & ~0x4); } }
        private bool Flag0x2_1 { get { return (F & 0x2) != 0; } set { F = value ? (byte)(F | 0x2) : (byte)(F & ~0x2); } }
        private bool FlagC { get { return (F & 0x1) != 0; } set { F = value ? (byte)(F | 0x1) : (byte)(F & ~0x1); } }

        private byte M { get { return memory[HL]; } set { memory[HL] = value; } }
        private byte Data8 { get { return memory[PC]; } }
        private ushort Data16 { get { return BitConverter.ToUInt16(memory, PC); } }

        public Cpu(i8080Memory memory, i8080IObus iobus) {
            this.memory = memory.Mem;
            this.iobus = iobus;
            Flag0x2_1 = true; // always on flag
        }

        public void exe() {

            byte opcode = memory[PC++];
            //debug(opcode);

            cycles += cyclesValue[opcode];

            switch (opcode) {
                case 0x00: break;                           //NOP 	    4 	1 	------ 	00 	No Operation
                case 0x01: BC = Data16; PC += 2; break;     //LD BC,NN 	10 	3 	01 XX XX Load (16-bit) 	dst=src 
                case 0x02: memory[BC] = A; break;           //STAX B	1		(BC) <- A
                case 0x03: BC += 1; break;                  //INX B	1		BC <- BC+1
                case 0x04: B = INR(B); break;               //INR B	1	Z, S, P, AC	B <- B+1
                case 0x05: B = DCR(B); break;               //DCR B 1	Z, S, P, AC	B <- B-1
                case 0x06: B = Data8; PC += 1; break;       //MVI B, D8	2		B <- byte 2

                case 0x07: //RLC	1	CY	A = A << 1; bit 0 = prev bit 7; CY = prev bit 7
                    FlagC = ((A & 0x80) != 0);
                    A = (byte)((A << 1) | (A >> 7));
                    break;

                case 0x08: break;                            //NOP
                case 0x09: DAD(BC); break;                   //DAD B	1	CY	HL = HL + BC
                case 0x0A: A = memory[BC]; break;            //LDAX B	1		A <- (BC)
                case 0x0B: BC -= 1; break;                   //DCX B	1		BC = BC-1
                case 0x0C: C = INR(C); break;                //INR C	1	Z, S, P, AC	C <- C+1
                case 0x0D: C = DCR(C); break;                //DEC C 	4 	1 		Z, S, P, AC 0D Decrement (8-bit) 	s=s-1
                case 0x0E: C = Data8; PC += 1; ; break;      //MVI C,D8	2		C <- byte 2

                case 0x0F: //RRC	1	CY	A = A >> 1; bit 7 = prev bit 0; CY = prev bit 0
                    FlagC = ((A & 0x1) != 0);
                    A = (byte)((A >> 1) | (A << 7));
                    break;

                case 0x10: break;                             //NOP
                case 0x11: DE = Data16; PC += 2; ; break;     //LXI D, D16 3       D < -byte 3, E < -byte 2
                case 0x12: memory[DE] = A; break;             //STAX D	1		(DE) <- A
                case 0x13: DE += 1; break;                    //INX D	1		DE <- DE + 1
                case 0x14: D = INR(D); break;                 //INR D	1	Z, S, P, AC	D <- D+1
                case 0x15: D = DCR(D); break;                 //DCR D	1	Z, S, P, AC	D <- D-1
                case 0x16: D = Data8; PC += 1; ; break;       //MVI D, D8	2		D <- byte 2

                case 0x17://RAL	1	CY	A = A << 1; bit 0 = prev CY; CY = prev bit 7
                    bool prevC = FlagC;
                    FlagC = ((A & 0x80) != 0);
                    A = (byte)((A << 1) | (prevC ? 1 : 0));
                    break;

                case 0x18: break;                              //NOP
                case 0x19: DAD(DE); break;                     //DAD D	1	CY	HL = HL + DE
                case 0x1A: A = memory[DE]; break;              //LDAX D	1		A <- (DE)
                case 0x1B: DE -= 1; break;                     //DCX D	1		DE = DE-1
                case 0x1C: E = INR(E); break;                  //INR E	1	Z, S, P, AC	E <- E+1
                case 0x1D: E = DCR(E); break;                  //DCR E	1	Z, S, P, AC	E <- E-1
                case 0x1E: E = Data8; PC += 1; ; break;        //MVI E,D8	2		E <- byte 2

                case 0x1F://RAR	1	CY	A = A >> 1; bit 7 = prev bit 7; CY = prev bit 0
                    bool preC = FlagC;
                    FlagC = ((A & 0x1) != 0);
                    A = (byte)((A >> 1) | (preC ? 0x80 : 0));
                    break;

                case 0x20: break;                              //NOP
                case 0x21: HL = Data16; PC += 2; ; break;      //LXI H,D16	3		H <- byte 3, L <- byte 2
                case 0x22: memory[Data16] = L; memory[Data16 + 1] = H; PC += 2; ; break; //SHLD adr	3		(adr) <-L; (adr+1)<-H
                case 0x23: HL += 1; break;                     //INX H	1		HL <- HL + 1
                case 0x24: H = INR(H); break;                  //INR H	1	Z, S, P, AC	H <- H+1
                case 0x25: H = DCR(H); break;                  //DCR H	1	Z, S, P, AC	H <- H-1
                case 0x26: H = Data8; PC += 1; ; break;        //LD H,N 	7 	2 	26 XX Load (8-bit) 	dst=src 

                case 0x27: break; //DAA	1		special //Skiping (PENDING IMPLEMENTATION)

                case 0x28: break;                               //NOP
                case 0x29: DAD(HL); break;                      //DAD H	1	CY	HL = HL + HL
                case 0x2A: L = memory[Data16]; H = memory[Data16 + 1]; PC += 2; ; break; //LHLD adr	3		L <- (adr); H<-(adr+1)
                case 0x2B: HL -= 1; break;                      //DCX H	1		HL = HL-1
                case 0x2C: L = INR(L); break;                   //INR L	1	Z, S, P, AC	L <- L+1
                case 0x2D: L = DCR(L); break;                   //DCR L	1	Z, S, P, AC	L <- L-1
                case 0x2E: L = Data8; PC += 1; ; break;         //MVI L, D8	2		L <- byte 2

                case 0x2F: A = (byte)~A; break;                 //CMA	1		A <- !A

                case 0x30: break;                               //NOP
                case 0x31: SP = Data16; PC += 2; ; break;       //LXI SP, D16	3		SP.hi <- byte 3, SP.lo <- byte 2
                case 0x32: memory[Data16] = A; PC += 2; break;  //STA adr	3		(adr) <- A
                case 0x33: SP += 1; break;                      //INX SP	1		SP = SP + 1
                case 0x34: M = INR(M); break;                   //INR M	1	Z, S, P, AC	(HL) <- (HL)+1
                case 0x35: M = DCR(M); break;                   //DCR M	1	Z, S, P, AC	(HL) <- (HL)-1
                case 0x36: M = Data8; PC += 1; ; break;         //MVI M,D8	2		(HL) <- byte 2

                case 0x37: FlagC = true; break;                 //STC	1	CY	CY = 1

                case 0x38: break;                               //NOP
                case 0x39: DAD(SP); break;                      //DAD SP	1	CY	HL = HL + SP
                case 0x3A: A = memory[Data16]; PC += 2; break;  //LDA adr	3		A <- (adr)
                case 0x3B: SP -= 1; break;                      //DCX SP	1		SP = SP-1
                case 0x3C: A = INR(A); break;                   //INR A	1	Z, S, P, AC	A <- A+1
                case 0x3D: A = DCR(A); break;                   //DCR A	1	Z, S, P, AC	A <- A-1
                case 0x3E: A = Data8; PC += 1; ; break;         //MVI A,D8	2		A <- byte 2

                case 0x3F: FlagC = !FlagC; break;               //CMC	1	CY	CY=!CY

                case 0x40: /*B = B;*/ break; //MOV B,B	1	B <- B
                case 0x41: B = C; break; //MOV B,C	1		B <- C
                case 0x42: B = D; break; //MOV B,D	1		B <- D
                case 0x43: B = E; break; //MOV B,E	1		B <- E
                case 0x44: B = H; break; //MOV B,H	1		B <- H
                case 0x45: B = L; break; //MOV B,L	1		B <- L
                case 0x46: B = M; break; //MOV B,M	1		B <- (HL)
                case 0x47: B = A; break; //MOV B,A	1		B <- A

                case 0x48: C = B; break; //MOV C,B	1		C <- B
                case 0x49: /*C = C;*/ break; //MOV C,C	1	C <- C
                case 0x4A: C = D; break; //MOV C,D	1		C <- D
                case 0x4B: C = E; break; //MOV C,E	1		C <- E
                case 0x4C: C = H; break; //MOV C,H	1		C <- H
                case 0x4D: C = L; break; //MOV C,L	1		C <- L
                case 0x4E: C = M; break; //MOV C,M	1		C <- (HL)
                case 0x4F: C = A; break; //MOV C,A	1		C <- A

                case 0x50: D = B; break; //MOV D,B	1		D <- B
                case 0x51: D = C; break; //MOV D,C	1		D <- C
                case 0x52: /*D = D;*/ break; //MOV D,D	1	D <- D
                case 0x53: D = E; break; //MOV D,E	1		D <- E
                case 0x54: D = H; break; //MOV D,H	1		D <- H
                case 0x55: D = L; break; //MOV D,L	1		D <- L
                case 0x56: D = M; break; //LD D,(HL) 	7 	1 	56 Load (8-bit) 	dst=src 
                case 0x57: D = A; break; //MOV D,A	1		D <- A

                case 0x58: E = B; break; //MOV E,B	1		E <- B
                case 0x59: E = C; break; //MOV E,C	1		E <- C
                case 0x5A: E = D; break; //MOV E,D	1		E <- D
                case 0x5B: /*E = E;*/ break; //MOV E,E	1	E <- E
                case 0x5C: E = H; break; //MOV E,H	1		E <- H
                case 0x5D: E = L; break; //MOV E,L	1		E <- L
                case 0x5E: E = M; break; //LD E,(HL) 	7 	1 	5E Load (8-bit) 	dst=src
                case 0x5F: E = A; break; //MOV E,A	1		E <- A

                case 0x60: H = B; break; //MOV H,B	1		H <- B
                case 0x61: H = C; break; //MOV H,C	1		H <- C
                case 0x62: H = D; break; //MOV H,D	1		H <- D
                case 0x63: H = E; break; //MOV H,E	1		H <- E
                case 0x64: /*H = H;*/ break; //MOV H,H	1	H <- H
                case 0x65: H = L; break; //MOV H,L	1		H <- L
                case 0x66: H = M; break; //LD H,(HL) 	7 	1 	66 Load (8-bit) 	dst=src
                case 0x67: H = A; break; //MOV H,A	1		H <- A

                case 0x68: L = B; break; //MOV L,B	1		L <- B
                case 0x69: L = C; break; //MOV L,C	1		L <- C
                case 0x6A: L = D; break; //MOV L,D	1		L <- D
                case 0x6B: L = E; break; //MOV L,E	1		L <- E
                case 0x6C: L = H; break; //MOV L,H	1		L <- H
                case 0x6D: /*L=L;*/ break; //MOV L,L	1	L <- L
                case 0x6E: L = M; break;//MOV L,M	1		L <- (HL)
                case 0x6F: L = A; break;//	MOV L,A	1		L <- A

                case 0x70: M = B; break;//MOV M,B	1		(HL) <- B
                case 0x71: M = C; break;//MOV M,C	1		(HL) <- C
                case 0x72: M = D; break;//MOV M,D	1		(HL) <- D
                case 0x73: M = E; break;//MOV M,E	1		(HL) <- E
                case 0x74: M = H; break;//	MOV M,H	1		(HL) <- H
                case 0x75: M = L; break;//MOV M,L	1		(HL) <- L
                case 0x76: PC--; break;//HLT	1		Halts Cpu until interruption
                case 0x77: M = A; break; //MOV M,A	1		(HL) <- A

                case 0x78: A = B; break; //MOV A,B	1		A <- B
                case 0x79: A = C; break; //MOV A,C	1		A <- C
                case 0x7A: A = D; break; //MOV A,D	1		A <- D
                case 0x7B: A = E; break; //MOV A,E	1		A <- E
                case 0x7C: A = H; break; //MOV A,H	1		A <- H
                case 0x7D: A = L; break; //MOV A,L	1		A <- L
                case 0x7E: A = M; break; //LD A,(HL) 	7 	1 	7E Load (8-bit) 	dst=src
                case 0x7F: /*A = A;*/ break; //MOV A,A	1	A <- A

                case 0x80: ADD(B); break; //ADD B	1	Z, S, P, CY, AC	A <- A + B
                case 0x81: ADD(C); break; //ADD C	1	Z, S, P, CY, AC	A <- A + C
                case 0x82: ADD(D); break; //ADD D	1	Z, S, P, CY, AC	A <- A + D
                case 0x83: ADD(E); break; //ADD E	1	Z, S, P, CY, AC	A <- A + E
                case 0x84: ADD(H); break; //ADD H	1	Z, S, P, CY, AC	A <- A + H
                case 0x85: ADD(L); break; //ADD L	1	Z, S, P, CY, AC	A <- A + L
                case 0x86: ADD(M); break; //ADD M	1	Z, S, P, CY, AC	A <- A + (HL)
                case 0x87: ADD(A); break; //ADD A	1	Z, S, P, CY, AC	A <- A + A

                case 0x88: ADC(B); break; //ADC B	1	Z, S, P, CY, AC	A <- A + B + CY
                case 0x89: ADC(C); break; //ADC C	1	Z, S, P, CY, AC	A <- A + C + CY
                case 0x8A: ADC(D); break; //ADC D	1	Z, S, P, CY, AC	A <- A + D + CY
                case 0x8B: ADC(E); break; //ADC E	1	Z, S, P, CY, AC	A <- A + E + CY
                case 0x8C: ADC(H); break; //ADC H	1	Z, S, P, CY, AC	A <- A + H + CY
                case 0x8D: ADC(L); break; //ADC L	1	Z, S, P, CY, AC	A <- A + L + CY
                case 0x8E: ADC(M); break; //ADC M	1	Z, S, P, CY, AC	A <- A + (HL) + CY
                case 0x8F: ADC(A); break; //ADC A	1	Z, S, P, CY, AC	A <- A + A + CY

                case 0x90: SUB(B); break; //SUB B	1	Z, S, P, CY, AC	A <- A - B
                case 0x91: SUB(C); break; //SUB C	1	Z, S, P, CY, AC	A <- A - C
                case 0x92: SUB(D); break; //SUB D	1	Z, S, P, CY, AC	A <- A - D
                case 0x93: SUB(E); break; //SUB E	1	Z, S, P, CY, AC	A <- A - E
                case 0x94: SUB(H); break; //SUB H	1	Z, S, P, CY, AC	A <- A + H
                case 0x95: SUB(L); break; //SUB L	1	Z, S, P, CY, AC	A <- A - L
                case 0x96: SUB(M); break; //SUB M	1	Z, S, P, CY, AC	A <- A - (HL)
                case 0x97: SUB(A); break; //SUB A	1	Z, S, P, CY, AC	A <- A - A

                case 0x98: SBB(B); break; //SBB B	1	Z, S, P, CY, AC	A <- A - B - CY
                case 0x99: SBB(C); break; //SBB C	1	Z, S, P, CY, AC	A <- A - C - CY
                case 0x9A: SBB(D); break; //SBB D	1	Z, S, P, CY, AC	A <- A - D - CY
                case 0x9B: SBB(E); break; //SBB E	1	Z, S, P, CY, AC	A <- A - E - CY
                case 0x9C: SBB(H); break; //SBB H	1	Z, S, P, CY, AC	A <- A - H - CY
                case 0x9D: SBB(L); break; //SBB L	1	Z, S, P, CY, AC	A <- A - L - CY
                case 0x9E: SBB(M); break; //SBB M	1	Z, S, P, CY, AC	A <- A - (HL) - CY
                case 0x9F: SBB(A); break; //SBB A	1	Z, S, P, CY, AC	A <- A - A - CY

                case 0xA0: ANA(B); break; //ANA B	1	Z, S, P, CY, AC	A <- A & B
                case 0xA1: ANA(C); break; //ANA C	1	Z, S, P, CY, AC	A <- A & C
                case 0xA2: ANA(D); break; //ANA D	1	Z, S, P, CY, AC	A <- A & D
                case 0xA3: ANA(E); break; //ANA E	1	Z, S, P, CY, AC	A <- A & E
                case 0xA4: ANA(H); break; //ANA H	1	Z, S, P, CY, AC	A <- A & H
                case 0xA5: ANA(L); break; //ANA L	1	Z, S, P, CY, AC	A <- A & L
                case 0xA6: ANA(M); break; //ANA M	1	Z, S, P, CY, AC	A <- A & (HL)
                case 0xA7: ANA(A); break; //ANA A	1	Z, S, P, CY, AC	A <- A & A

                case 0xA8: XRA(B); break; //XRA B	1	Z, S, P, CY, AC	A <- A ^ B
                case 0xA9: XRA(C); break; //XRA C	1	Z, S, P, CY, AC	A <- A ^ C
                case 0xAA: XRA(D); break; //XRA D	1	Z, S, P, CY, AC	A <- A ^ D
                case 0xAB: XRA(E); break; //XRA E	1	Z, S, P, CY, AC	A <- A ^ E
                case 0xAC: XRA(H); break; //XRA H	1	Z, S, P, CY, AC	A <- A ^ H
                case 0xAD: XRA(L); break; //XRA L	1	Z, S, P, CY, AC	A <- A ^ L
                case 0xAE: XRA(M); break; //XRA M	1	Z, S, P, CY, AC	A <- A ^ (HL)
                case 0xAF: XRA(A); break; //XRA A	1	Z, S, P, CY, AC	A <- A ^ A

                case 0xB0: ORA(B); break; //ORA B	1	Z, S, P, CY, AC	A <- A | B
                case 0xB1: ORA(C); break; //ORA C	1	Z, S, P, CY, AC	A <- A | C
                case 0xB2: ORA(D); break; //ORA D	1	Z, S, P, CY, AC	A <- A | D
                case 0xB3: ORA(E); break; //ORA E	1	Z, S, P, CY, AC	A <- A | E
                case 0xB4: ORA(H); break; //ORA H	1	Z, S, P, CY, AC	A <- A | H
                case 0xB5: ORA(L); break; //ORA L	1	Z, S, P, CY, AC	A <- A | L
                case 0xB6: ORA(M); break; //ORA M	1	Z, S, P, CY, AC	A <- A | (HL)
                case 0xB7: ORA(A); break; //ORA A	1	Z, S, P, CY, AC	A <- A | A

                case 0xB8: CMP(B); break; //CMP B	1	Z, S, P, CY, AC	A - B
                case 0xB9: CMP(C); break; //CMP C	1	Z, S, P, CY, AC	A - C
                case 0xBA: CMP(D); break; //CMP D	1	Z, S, P, CY, AC	A - D
                case 0xBB: CMP(E); break; //CMP E	1	Z, S, P, CY, AC	A - E
                case 0xBC: CMP(H); break; //CMP H	1	Z, S, P, CY, AC	A - H
                case 0xBD: CMP(L); break; //CMP L	1	Z, S, P, CY, AC	A - L
                case 0xBE: CMP(M); break; //CMP M	1	Z, S, P, CY, AC	A - (HL)
                case 0xBF: CMP(A); break; //CMP A	1	Z, S, P, CY, AC	A - A

                case 0xC0: RETURN(!FlagZ); break;          //RNZ	1		if NZ, RET

                case 0xC1: BC = POP(); break;              //POP BC 	10 	1 	C1 Pop 	qq=(SP)+
                case 0xC2: JUMP(!FlagZ); break;            //JNZ adr	3		if NZ, PC <- adr
                case 0xC3: JUMP(true); break;              //JMP adr
                case 0xC4: CALL(!FlagZ); break;            //CNZ adr	3		if NZ, CALL adr
                case 0xC5: PUSH(BC); break;                //PUSH BC 	11 	1 	C5 Push 	(SP)=qq 
                case 0xC6: ADD(Data8); PC += 1; break;     //ADI D8	2	Z, S, P, CY, AC	A <- A + byte
                case 0xC7: RST(0x0); break;                //RST 0	1		CALL $0

                case 0xC8: RETURN(FlagZ); break;           // RZ	1		if Z, RET
                case 0xC9: RETURN(true); break;            // RET	1		PC.lo <- (sp); PC.hi<-(sp+1); SP <- SP+2
                case 0xCA: JUMP(FlagZ); break;             // JZ adr 3       if Z, PC < -adr
                case 0xCB: JUMP(true); break;              //JMP adr Alt Instruction C3 Clone
                case 0xCC: CALL(FlagZ); break;             //CZ adr	3		if Z, CALL adr

                case 0xCD:                                 //CALL adr	3		(SP-1)<-PC.hi;(SP-2)<-PC.lo;PC=adrç
                    /*
                    if (5 == Data16) {  //DEBUG REMANENTS FOR CPU TESTS
                        if (C == 9) {
                            int i = 0;  //skip the prefix bytes     era 3
                            while ((char)memory[DE + i] != '$')
                                Console.Write((char)memory[DE + i++]);
                            //Console.WriteLine("");
                            //Console.WriteLine("devCounter " +devCounter); //DEBUG
                            //debug(opcode);
                        } else if (C == 2) {
                            //saw this in the inspected code, never saw it called    
                            Console.Write((char)E);
                        }
                        PC += 2; ; // OJO
                    } else if (0 == Data16) {
                        Debug.WriteLine("KILL MODE ON");
                    } else {
                    */
                        CALL(true);
                    //}
                    break;

                case 0xCE: ADC(Data8); PC += 1; ; break; //ACI D8	2	Z, S, P, CY, AC	A <- A + data + CY

                case 0xCF: RST(0x8); break;         //RST 1	1		CALL $8

                case 0xD0: RETURN(!FlagC); break;   //RNC	1		if NCY, RET
                case 0xD1: DE = POP(); break;       //POP DE 	10 	1 	D1 Pop 	qq=(SP)+ 
                case 0xD2: JUMP(!FlagC); break;     //JP NC,$NN 	10/1 	3 	D2 XX XX If Carry = 0

                case 0xD3: iobus.Write(Data8, A); PC += 1; break;              //OUT (N),A 	11 	2 	------ 	D3 XX 	Output 	(n)=A //TODO OUTPUT

                case 0xD4: CALL(!FlagC); break;     //CNC adr	3		if NCY, CALL adr

                case 0xD5: PUSH(DE); break;         //PUSH D	1		(sp-2)<-E; (sp-1)<-D; sp <- sp - 2
                case 0xD6: SUB(Data8); PC += 1; ; break; //SUI D8	2	Z, S, P, CY, AC	A <- A - data

                case 0xD7: RST(0x10); break;        //RST 2	1		CALL $10

                case 0xD8: RETURN(FlagC); break;    // RC	1		if CY, RET
                case 0xD9: RETURN(true); break;     // RET	1		PC.lo <- (sp); PC.hi<-(sp+1); SP <- SP+2 // Alt Instruction C9 Clone
                case 0xDA: JUMP(FlagC); break;      // JC adr	3		if CY, PC<-adr

                case 0xDB: A = iobus.Read(Data8); PC += 1; break; //IN A,(N) 	11 	2 	------ 	DB XX 	Input 	A=(n)

                case 0xDC: CALL(FlagC); break;      //	CC adr	3		if CY, CALL adr
                case 0xDD: CALL(true); break;       // Alt instruction CD Clone
                case 0xDE: SBB(Data8); PC += 1; ; break; // SBI D8	2	Z, S, P, CY, AC	A <- A - data - CY

                case 0xDF: RST(0x18); ; break;      // RST 3	1		CALL $18

                case 0xE0: RETURN(!FlagP); break;   //	RPO	1		if PO, RET
                case 0xE1: HL = POP(); break;       //POP H	1		L <- (sp); H <- (sp+1); sp <- sp+2
                case 0xE2: JUMP(!FlagP); break;     //JPO adr	3		if PO, PC <- adr
                case 0xE3:                          //XTHL   1       L <-> (SP); H <-> (SP + 1)
                    L ^= memory[SP]; memory[SP] ^= L; L ^= memory[SP];
                    H ^= memory[SP + 1]; memory[SP + 1] ^= H; H ^= memory[SP + 1];
                    break;
                case 0xE4: CALL(!FlagP); break;     //CPO adr	3		if PO, CALL adr
                case 0xE5: PUSH(HL); break;         // PUSH H	1		(sp-2)<-L; (sp-1)<-H; sp <- sp - 2
                case 0xE6: ANA(Data8); PC += 1; ; break; //ANI D8	2	Z, S, P, CY, AC	A <- A & data
                case 0xE7: RST(0x20); break;        //RST 4	1		CALL $20
                case 0xE8: RETURN(FlagP); break;    //RPE	1		if PE, RET
                case 0xE9: PC = HL; break;          //PCHL	1		PC.hi <- H; PC.lo <- L
                case 0xEA: JUMP(FlagP); break;      //JPE adr	3		if PE, PC <- adr
                case 0xEB: HL ^= DE; DE ^= HL; HL ^= DE; break; //XCHG	1		H <-> D; L <-> E
                case 0xEC: CALL(FlagP); break;      //CPE adr	3		if PE, CALL adr
                case 0xED: CALL(true); break;       // Alt instruction CD Clone
                case 0xEE: XRA(Data8); PC += 1; ; break; //XRI D8	2	Z, S, P, CY, AC	A <- A ^ data

                case 0xEF: RST(0x28); break;        //RST 5	1		CALL $28
                case 0xF0: RETURN(!FlagS); break;   //RP	1		if P, RET
                case 0xF1: AF = POP(); break;       // POP PSW	1		flags <- (sp); A <- (sp+1); sp <- sp+2
                case 0xF2: JUMP(!FlagS); break;     //	JP adr	3		if P=1 PC <- adr
                case 0xF3: interruptPin = false; break; //DI	1       Disable Interrupts
                case 0xF4: CALL(!FlagS); break;     //CP adr	3		if P, PC <- adr
                case 0xF5: PUSH(AF); break;         //PUSH AF 	11 	1 	------ 	F5 PUSH 	(SP)=qq
                case 0xF6: ORA(Data8); PC += 1; ; break; //ORI D8	2	Z, S, P, CY, AC	A <- A | data
                case 0xF7: RST(0x30); break;        //RST 6	1		CALL $30
                case 0xF8: RETURN(FlagS); break;    //RM	1		if M, RET
                case 0xF9: SP = HL; break;          //SPHL	1		SP=HL
                case 0xFA: JUMP(FlagS); break;      //JM adr	3		if M, PC <- adr
                case 0xFB: interruptPin = true; break; //EI 	4 	1 	Enable Interrupts 	
                case 0xFC: CALL(FlagS); break;      // CM adr	3		if M, CALL adr
                case 0xFD: CALL(true); break;       // Alt instruction CD Clone
                case 0xFE: CMP(Data8); PC += 1; ; break; //CPI D8	2	Z, S, P, CY, AC	A - data

                case 0xFF: RST(0x38); break; //RST 7	1		CALL $38
                default: warnUnsupportedOpcode(opcode); break;
            }
        }

        private byte INR(byte b) {
            int result = b + 1;
            FlagAC = (((b & 0xF) + 1) & 0x10) == 1;// pending refactor AC calc
            SetFlagsSZP(result);
            return (byte)result;
        }

        private byte DCR(byte b) {
            int result = b - 1;
            FlagAC = (((b & 0xF) - 1) & 0x10) == 1;// pending refactor AC calc
            SetFlagsSZP(result);
            return (byte)result;
        }

        private void ADD(byte b) {
            int result = A + b;
            FlagAC = (((A & 0xF) + (b & 0xF)) & 0x10) == 1; // pending refactor AC calc
            SetFlagC(result);
            SetFlagsSZP(result);
            A = (byte)result;
        }

        private void ADC(byte b) {
            int carry = FlagC ? 1 : 0;
            int result = A + b + carry;
            FlagAC = (((A & 0xF) + (b & 0xF) + carry) & 0x10) == 1; // pending refactor AC calc
            SetFlagC(result);
            SetFlagsSZP(result);
            A = (byte)result;
        }

        private void SUB(byte b) {
            int result = A - b;
            FlagAC = (((A & 0xF) - (b & 0xF)) & 0x10) == 1; // pending refactor AC calc
            SetFlagC(result);
            SetFlagsSZP(result);
            A = (byte)result;
        }

        private void SBB(byte b) {
            int carry = FlagC ? 1 : 0;
            int result = A - b - carry;
            FlagAC = (((A & 0xF) - (b & 0xF) - carry) & 0x10) == 1; // pending refactor AC calc
            SetFlagC(result);
            SetFlagsSZP(result);
            A = (byte)result;
        }

        private void ANA(byte b) {
            byte result = (byte)(A & b);
            FlagC = false;
            SetFlagsSZP(result);
            A = result;
        }

        private void XRA(byte b) {
            byte result = (byte)(A ^ b);
            FlagC = false;
            SetFlagsSZP(result);
            A = result;
        }

        private void ORA(byte b) {
            byte result = (byte)(A | b);
            FlagC = false;
            SetFlagsSZP(result);
            A = result;
        }

        private void CMP(byte b) {
            int result = A - b;
            FlagAC = (((A & 0xF) - (b & 0xF)) & 0x10) == 1; // pending refactor AC calc
            SetFlagC(result);
            SetFlagsSZP(result);
        }

        private void DAD(ushort w) {
            int result = HL + w;
            FlagC = result >> 16 != 0; //Special FlagC as short value involved
            HL = (ushort)result;
        }

        private void RETURN(bool flag) {
            PC = flag ? POP() : PC;
        }

        private void CALL(bool flag) {
            if (flag) {
                PUSH((ushort)(PC + 2));
                PC = Data16;
            } else {
                PC += 2;
            }
        }

        private void JUMP(bool flag) {
            PC = flag ? Data16 : PC += 2; ;
        }

        private void RST(byte b) {
            PUSH((ushort)(PC + 2));
            PC = b;
        }

        private void PUSH(ushort d16) {// (SP - 1) < -PC.hi; (SP - 2) < -PC.lo
            SP -= 2;
            byte[] bytes = BitConverter.GetBytes(d16);
            Array.Copy(bytes, 0, memory, SP, bytes.Length);
            //Console.WriteLine("stack PUSH = " + d16.ToString("x4") + " SP = " + SP.ToString("x4") + " value" + BitConverter.ToUInt16(memory, SP).ToString("x4"));
        }

        private ushort POP() {
            ushort ret = combineRegisters(memory[SP + 1], memory[SP]);
            //Console.WriteLine("stack POP = " + ret.ToString("x4") + " SP = " + SP.ToString("x4") + " value" + BitConverter.ToUInt16(memory, PC).ToString("x4"));
            SP += 2;
            return ret;
        }

        private ushort combineRegisters(byte b1, byte b2) {
            return (ushort)(b1 << 8 | b2);
        }

        private void SetFlagC(int i) {
            FlagC = (i >> 8) != 0;
        }

        private void SetFlagsSZP(int i) {
            byte b = (byte)i;
            FlagS = (b & 0x80) != 0;
            FlagZ = b == 0;
            FlagP = parity(b);
        }

        private void SetFlagAC(int i) {
            //FlagA Pending to be implemented (only opcode 0x27 DAA needs it)
            //pending refactor
        }

        private bool parity(byte b) {
            byte bits = 0;
            for (int i = 0; i < 8; i++) {
                if ((b & 0x80 >> i) != 0) {
                    bits += 1;
                }
            }
            return (bits % 2 == 0);
        }

        public void handleInterrupt(byte b) {
            if (interruptPin) {
                PUSH(PC);
                PC = (ushort)(8 * b);
                interruptPin = false;
            }
        }

        private void warnUnsupportedOpcode(byte opcode) {
            Console.WriteLine((PC - 1).ToString("x4") + " Unsupported operation " + opcode.ToString("x2"));
        }

        public int dev;
        private void debug(byte opcode) {
            Console.WriteLine("cycle" + dev++ + " " + (PC - 1).ToString("x4") + " " + SP.ToString("x4") + " AF: " + A.ToString("x2") + "" + F.ToString("x2")
                + " BC: " + B.ToString("x2") + "" + C.ToString("x2") + " DE: " + D.ToString("x2") + "" + E.ToString("x2") + " HL: " + H.ToString("x2") + "" + L.ToString("x2")
                + " op " + opcode.ToString("x2") + " next16 " + Data16.ToString("x4"));
        }


    }
}