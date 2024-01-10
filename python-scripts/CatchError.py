import time

print("2 seconds before error...", flush=True)
time.sleep(2)
raise Exception("This is a test exception!")