SELECT
  path,
  directory,
  filename,
  inode,
  uid,
  gid,
  mode,
  size,
  atime,
  mtime,
  ctime,
  btime,
  hard_links,
  symlink,
  type,
  attributes
FROM
  file
WHERE
  (
    file.path LIKE "C:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "D:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "E:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "F:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "G:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "H:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "I:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "J:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "K:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "L:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "M:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "N:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "O:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "P:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "Q:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "R:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "S:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "T:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "U:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "V:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "W:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "X:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "Y:\%%\SolarWinds.Orion.Core.BusinessLayer.dll" OR
    file.path LIKE "Z:\%%\SolarWinds.Orion.Core.BusinessLayer.dll"
      )
    AND
      (
    file.product_version IN (
      "2020.2.5300.12432",
      "2020.2.5200.12394",
      "2020.4.100.478",
      "2020.2.100.11831",
      "2020.2.100.12219",
      "2019.4.5200.9083"
    )
  )
