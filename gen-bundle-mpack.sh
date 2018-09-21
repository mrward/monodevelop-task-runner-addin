# Cake
cp ../monodevelop-cake-task-runner/bin/MonoDevelop.CakeTaskRunner.dll src/MonoDevelop.TaskRunnersBundle/
cp ../monodevelop-cake-task-runner/bin/INIFileParser.dll src/MonoDevelop.TaskRunnersBundle/

# Grunt
mkdir src/MonoDevelop.TaskRunnersBundle/GruntScript
cp ../monodevelop-grunt-task-runner/bin/MonoDevelop.GruntTaskRunner.dll src/MonoDevelop.TaskRunnersBundle/
cp ../monodevelop-grunt-task-runner/bin/GruntScript/GruntTaskList.js src/MonoDevelop.TaskRunnersBundle/GruntScript/GruntTaskList.js

# Gulp
cp ../monodevelop-gulp-task-runner/bin/MonoDevelop.GulpTaskRunner.dll src/MonoDevelop.TaskRunnersBundle/

# TypeScript
cp ../monodevelop-typescript-task-runner/bin/MonoDevelop.TypeScriptTaskRunner.dll src/MonoDevelop.TaskRunnersBundle/

# Build bundle .mpack
mono /Applications/Visual\ Studio.app/Contents/Resources/lib/monodevelop/bin/vstool.exe setup pack src/MonoDevelop.TaskRunnersBundle/MonoDevelop.TaskRunnersBundle.addin.xml