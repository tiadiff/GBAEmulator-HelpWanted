import struct

with open('bin/Release/net8.0-windows/rom/gba_bios.bin', 'rb') as f:
    f.seek(0x00)
    d = f.read(0x200)
    for i in range(0, len(d), 4):
        print(f"{i:08X}: {struct.unpack('<I', d[i:i+4])[0]:08X}")
    val = struct.unpack('<I', d[i:i+4])[0]
    print(f"{0x180 + i:03X}: {val:08X}")
