
if [ -z $1 ]; then
    echo "No tag version supplied. './tag-bundle-addins.sh 0.1"
    exit
fi
  

TAG="$1"-task-runner-bundle

echo "git tag $TAG"

pushd () {
    command pushd "$@" > /dev/null
}

popd () {
    command popd "$@" > /dev/null
}

# Cake
echo 'Tagging Cake'
pushd ../monodevelop-cake-task-runner
git tag $TAG
popd

# Grunt
echo 'Tagging Grunt'
pushd ../monodevelop-grunt-task-runner
git tag $TAG
popd

# Gulp
pushd ../monodevelop-gulp-task-runner
git tag $TAG
popd

# NPM
echo 'Tagging NPM'
pushd ../NpmTaskRunner
git tag $TAG
popd

# TypeScript
echo 'Tagging TypeScript'
pushd ../monodevelop-typescript-task-runner
git tag $TAG
popd

echo 'Done'
