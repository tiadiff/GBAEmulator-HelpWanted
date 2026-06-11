import struct

with open('bin/Debug/net8.0-windows/normmatt.bin', 'rb') as f:
    f.seek(0x180)
    d = f.read(0x80)
    
for i in range(0, len(d), 4):
    val = struct.unpack('<I', d[i:i+4])[0]
    print(f"{0x180 + i:03X}: {val:08X}")
