
namespace BlueStorm.intel8080CpuCore {
    interface i8080IObus{
        byte Read(byte b);
        void Write(byte b, byte A);
    }
}
