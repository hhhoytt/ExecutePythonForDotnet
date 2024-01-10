import time
import gpiozero

cpu = gpiozero.CPUTemperature()

for x in range(5):
    print(cpu.temperature, flush=True)
    time.sleep(1)
