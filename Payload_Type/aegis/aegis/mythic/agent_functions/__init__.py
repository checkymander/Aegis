import glob
import os.path
from pathlib import Path
from importlib import import_module, invalidate_caches
import sys
# Get file paths of all modules.

currentPath = Path(__file__)