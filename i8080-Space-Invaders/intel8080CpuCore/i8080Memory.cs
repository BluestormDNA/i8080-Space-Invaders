
namespace BlueStorm.intel8080CpuCore {
    interface i8080Memory {

        byte[] Mem { get; set; }
        void LoadRom();
    }
}
