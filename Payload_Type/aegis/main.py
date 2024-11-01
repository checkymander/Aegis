import mythic_container
from aegis.mythic import *
import subprocess

#I just need it to do something
#Kick off an initial load and obfuscation of the task plugins
mythic_container.mythic_service.start_and_run_forever()