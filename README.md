# Space Invaders Intel 8080 Arcade Emulator

This is a C# coded Space Invaders Arcade Emulator that used the Intel 8080 CPU. It was primary coded as a learning exercise.

The CPU core can be reused as long as the memory ad iobus interfaces are implemented.
It's not a timing sensitive implementation as branch cycles are not accurate because they dont have in account if the branch are executed or not.

All the CPU opcodes are implemented and it passes cpudiag.bin and 8080EX1 tests.




## Playing Space Invaders

The unzipped romset must be in the same directory as the executable.

It can be played with the following keys:

* Insert Coin: **1**
* Start: **Enter**
* Left: **Left** or **A**
* Right: **Right** or **D**
* Shoot: **Space**

> **Note:**  You need to provide your own unzipped Space Invaders Rom Set:
> "invaders.h", "invaders.g", "invaders.f", "invaders.e". This is the standard **MAME** Space Invaders rom set.

![si](https://user-images.githubusercontent.com/28767885/47822270-d16ecb00-dd63-11e8-9b66-28c6dbf2f8c7.png)
![si2](https://user-images.githubusercontent.com/28767885/47822269-d16ecb00-dd63-11e8-800e-5ef4b3b13fe3.png)
