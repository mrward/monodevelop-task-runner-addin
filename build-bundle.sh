# Cake
echo 'Building Cake'
msbuild /v:q /nologo /t:Rebuild ../monodevelop-cake-task-runner/src/MonoDevelop.CakeTaskRunner.sln

# Grunt
echo 'Building Grunt'
msbuild /v:q /nologo /t:Rebuild ../monodevelop-grunt-task-runner/src/MonoDevelop.GruntTaskRunner.sln

# Gulp
echo 'Building Gulp'
msbuild /v:q /nologo /t:Rebuild ../monodevelop-gulp-task-runner/src/MonoDevelop.GulpTaskRunner.sln

# NPM
echo 'Building NPM'
msbuild /v:q /nologo /t:Rebuild ../NpmTaskRunner/MonoDevelop.NpmTaskRunner.sln

# TypeScript
echo 'Building TypeScript'
msbuild /v:q /nologo /t:Rebuild ../monodevelop-typescript-task-runner/src/MonoDevelop.TypeScriptTaskRunner.sln

echo 'Done'
