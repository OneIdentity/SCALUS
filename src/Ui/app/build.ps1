if(test-path dist)
{
    rm -force -recurse dist
}
ng build --configuration production

if(test-path dist/app)
{
    "Copying build artifacts to ../Web for inclusion"
    foreach($item in (("main","js"),
                      ("polyfills","js"),
                      ("runtime", "js"),
                      ("favicon", "ico"),
                      ("styles", "css")))
    {
        $path = resolve-path "dist/app/$($item[0]).*"
        $target = "../Web/$($item[0]).$($item[1])"
        "Copying $($path)\* => $($target)"
        Copy-Item $path $target
    }

    $path = resolve-path "dist/app/assets"
    $target = resolve-path "../Web"
    "Copying $($path) => ../Web/assets"
    Copy-Item -Recurse -Path $path -Destination $target -Container -Force
}
else 
{
    "looks like the build failed"
}
