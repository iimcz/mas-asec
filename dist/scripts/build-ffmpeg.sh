#!/bin/bash

set -e

FFMPEG_VERSION="7.0.2"
FFMPEG_DEPS="libx264-dev libx265-dev libvpx-dev yasm"

die()
{
    printf '%s\n' "$*" >&2
    exit 1
}

BM_INSTRUCTIONS="Please download the Blackmagic DeckLink 12.4.1 SDK and extract the Linux subfolder to '/opt/BM_SDK' so that '/opt/BM_SDK/include' is a valid path."

check_bm_sdk()
{
    return `test -f /opt/BM_SDK/include/DeckLinkAPI.h`
}

fetch_ffmpeg_sources()
{
    wget https://www.ffmpeg.org/releases/ffmpeg-${FFMPEG_VERSION}.tar.xz
    tar xvJf ffmpeg-${FFMPEG_VERSION}.tar.xz
    return 0
}

install_ffmpeg_deps()
{
    echo "Will now install ffmpeg dependencies. This requires root privileges."
    sudo apt install $FFMPEG_DEPS
}

compile_ffmpeg()
{   
    pushd "ffmpeg-${FFMPEG_VERSION}"
    ./configure --prefix=/usr/local --extra-cflags=-I/opt/BM_SDK/include --enable-decklink \
    --enable-libx264 --enable-libx265 --enable-libvpx --enable-gpl --enable-nonfree
    make -j4
    popd
}

install_ffmpeg()
{
    echo "I will now install the compiled ffmpeg to /usr/local. This requires root privileges."
    pushd "ffmpeg-${FFMPEG_VERSION}"
    sudo make install
    popd
}

echo "This script will use the current working directory to download, extract"
echo "and compile Ffmpeg in. Please make sure you are okay with that before"
echo "continuing (feel free to interrupt the script otherwise)."
echo "Press [Enter] to continue..." && read
check_bm_sdk || die "$BM_INSTRUCTIONS"
fetch_ffmpeg_sources || die "!! Unable to fetch ffmpeg sources !!"
install_ffmpeg_deps || die "!! Unable to install dependencies for ffmpeg !!"
compile_ffmpeg || die "!! Unable to compile ffmpeg !!"
install_ffmpeg || die "!! Unable to install ffmpeg !!"