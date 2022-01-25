#!/bin/bash

set -m
apt-get install libfaketime
python3 faketime-server.py &
./start --standalone
