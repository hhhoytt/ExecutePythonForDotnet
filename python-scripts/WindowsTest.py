import time
import psutil

for x in range(5):
    print(psutil.virtual_memory().free, flush=True)
    time.sleep(1)
