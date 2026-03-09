REPODIR=`git rev-parse --show-toplevel`
RUNTIME_CONFIGS="appsettings*.json tools.json emulators.json platforms.json"
REMOTE="adept2"

pushd "$REPODIR"/backend

rm -rf bin/Release/net10.0/publish
dotnet publish --sc -a x64 --os linux -c Release

pushd bin/Release/net10.0/linux-x64/publish
rm -f publish.zip
rm -f $RUNTIME_CONFIGS # To not overwrite settings already present on the server...
zip -r publish.zip *
scp publish.zip $REMOTE:
ssh $REMOTE 'cd /opt/naki3-asec; sudo unzip -o ~/publish.zip; sudo chown -R asec:asec /opt/naki3-asec; sudo systemctl restart naki3-asec;'

popd
popd
