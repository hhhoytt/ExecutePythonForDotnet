import sys
import time
import random

print("Recieved " + str(len(sys.argv)) + " arguments", flush=True)

for val in sys.argv:
    print(" " + val, flush=True)
    time.sleep(random.randint(1,3))