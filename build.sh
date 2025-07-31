#!/bin/bash

# Publish the application
dotnet publish RunCat-Linux -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true

# Create the package directory structure
mkdir -p package/usr/bin
mkdir -p package/usr/share/applications
mkdir -p package/usr/share/icons/hicolor/scalable/apps

# Copy the executable
cp RunCat-Linux/bin/Release/net8.0/linux-x64/publish/RunCat-Linux package/usr/bin/runcat

# Copy the desktop file
cp runcat.desktop package/usr/share/applications/

# Copy the icon
cp RunCat-Linux/resources/light_cat_0.ico package/usr/share/icons/hicolor/scalable/apps/runcat.ico

# Create the deb package
dpkg-deb --build package
